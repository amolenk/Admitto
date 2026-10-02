using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.Shared;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.Services;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee;

internal sealed class RegisterAttendeeHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterAttendeeCommand, RegisterAttendeeResult>
{
    public async ValueTask<RegisterAttendeeResult> HandleAsync(
        RegisterAttendeeCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var email = EmailAddress.From(command.Email);
        var firstName = FirstName.From(command.FirstName);
        var lastName = LastName.From(command.LastName);
        var registerTicketTypeIds = command.RegisterTicketTypeIds.Select(TicketTypeId.From).ToList();
        var waitlistTicketTypeIds = command.WaitlistTicketTypeIds.Select(TicketTypeId.From).ToList();

        RegistrationTicketClassifier.EnsureNoDuplicateRequestedActions(
            registerTicketTypeIds, waitlistTicketTypeIds);

        var ticketedEvent = await writeStore.TicketedEvents
            .GetAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);
        var catalog = await writeStore.TicketCatalogs
            .GetAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);
        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(TicketCatalog.Errors.EventNotActive);
        catalog.EnsureEventActive();

        var now = timeProvider.GetUtcNow();
        AdditionalDetails additionalDetails = AdditionalDetails.Empty;
        if (registerTicketTypeIds.Count > 0)
        {
            additionalDetails = AdditionalDetails.Validate(
                command.AdditionalDetails,
                ticketedEvent.AdditionalDetailSchema);
        }

        Coupon? coupon = null;
        if (command.CouponCode is { } couponCode)
        {
            coupon = await writeStore.Coupons.GetAsync(
                c => c.EventId == eventId && c.TeamId == teamId && c.Code == CouponCode.From(couponCode),
                cancellationToken);
        }

        var (couponGrantedIds, publicRegisterIds) = RegistrationCouponHelpers.SplitCouponGranted(
            coupon, email, registerTicketTypeIds, now);
        var hasOtherChanges = publicRegisterIds.Count > 0 || waitlistTicketTypeIds.Count > 0;
        if (!RegistrationCouponHelpers.WindowBypassApplies(coupon, hasOtherChanges))
        {
            if (registerTicketTypeIds.Count > 0 || waitlistTicketTypeIds.Count > 0)
                ticketedEvent.EnsureRegistrationOpen(now);
        }

        if (registerTicketTypeIds.Count > 0 || waitlistTicketTypeIds.Count > 0)
        {
            // Coupon-only claims retain their historical domain bypass. Any public claim or waitlist join
            // remains subject to the event's normal self-service domain policy.
            if (coupon is null || hasOtherChanges)
                ticketedEvent.EnsureEmailDomainAllowed(email);
        }

        var existingRegistration = await writeStore.Registrations
            .SingleOrDefaultAsync(
                r => r.EventId == eventId && r.TeamId == teamId && r.Email == email,
                cancellationToken);

        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == eventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);
        var currentWaitlistIds = waitlists.Where(w => w.HasActiveEntry(email)).Select(w => w.Id).ToList();
        var outstandingOfferTicketTypeIds = await GetOutstandingOfferTicketTypeIdsAsync(
            waitlists, email, cancellationToken);

        // Pre-generated so the same id can back both the waitlist entries joined below and the registration
        // created afterward (waitlisted tickets are only known once that loop completes).
        var registrationId = existingRegistration?.Id ?? RegistrationId.New();

        var hasLiveRegistration = existingRegistration?.Status == RegistrationStatus.Registered
                                  || (existingRegistration?.Status == RegistrationStatus.Waitlisted
                                      && (currentWaitlistIds.Count > 0 || outstandingOfferTicketTypeIds.Count > 0));

        if (publicRegisterIds.Count > 0 && hasLiveRegistration)
            throw new BusinessRuleViolationException(AlreadyExistsError.Create<Registration>());

        if (couponGrantedIds.Count > 0 && existingRegistration?.Status == RegistrationStatus.Registered)
            throw new BusinessRuleViolationException(AlreadyExistsError.Create<Registration>());

        RegistrationTicketClassifier.EnsureRequestedTicketStatesMatch(
            catalog, publicRegisterIds, waitlistTicketTypeIds);
        RegistrationTicketClassifier.ValidateWaitlistRequests(catalog, waitlistTicketTypeIds);

        var publiclyClaimedTickets = catalog.Claim(publicRegisterIds, ClaimMode.Public);
        var couponClaimedTickets = coupon is null
            ? []
            : catalog.ClaimWithCoupon(couponGrantedIds, coupon);
        var tickets = publiclyClaimedTickets.Concat(couponClaimedTickets).ToList();

        if (coupon is not null)
        {
            await RegistrationCouponHelpers.ApplyRedemptionToWaitlistsAsync(
                writeStore, waitlists, catalog, coupon, email, couponGrantedIds, now, cancellationToken);
        }

        var waitlistsById = waitlists.ToDictionary(w => w.Id);
        foreach (var waitlistTicketTypeId in waitlistTicketTypeIds)
        {
            if (!waitlistsById.TryGetValue(waitlistTicketTypeId, out var waitlist))
            {
                waitlist = Waitlist.Create(eventId, waitlistTicketTypeId, teamId);
                await writeStore.Waitlists.AddAsync(waitlist, cancellationToken);
                waitlistsById[waitlistTicketTypeId] = waitlist;
            }

            waitlist.AddEntry(email, now, catalog, registrationId);
        }

        var waitlistedTickets = RegistrationCouponHelpers.DescribeActiveWaitlistEntries(
            waitlistsById.Values, catalog, email);

        Registration registration;
        if (existingRegistration is null)
        {
            registration = Registration.Create(
                ticketedEvent.TeamId,
                eventId,
                email,
                firstName,
                lastName,
                tickets,
                additionalDetails,
                now,
                waitlistedTickets,
                registrationId);
            await writeStore.Registrations.AddAsync(registration, cancellationToken);
        }
        else if (publicRegisterIds.Count > 0 || couponGrantedIds.Count > 0 || !hasLiveRegistration)
        {
            registration = existingRegistration;
            registration.Reset(firstName, lastName, tickets, additionalDetails, now, waitlistedTickets);
        }
        else
        {
            registration = existingRegistration;
            registration.ChangeTickets(
                registration.Tickets.ToList(),
                catalog.DescribeTicketTypes(currentWaitlistIds),
                catalog.DescribeTicketTypes(currentWaitlistIds.Union(waitlistTicketTypeIds)),
                now);
        }

        return new RegisterAttendeeResult(
            registration.Id.Value,
            registerTicketTypeIds.Select(id => id.Value).ToArray(),
            waitlistTicketTypeIds.Select(id => id.Value).ToArray());
    }

    private async ValueTask<IReadOnlyList<TicketTypeId>> GetOutstandingOfferTicketTypeIdsAsync(
        IReadOnlyList<Waitlist> eventWaitlists,
        EmailAddress email,
        CancellationToken cancellationToken)
    {
        var issuedCouponIds = eventWaitlists
            .SelectMany(w => w.Coupons)
            .Where(c => c.Status == WaitlistCouponStatus.Issued)
            .Select(c => c.Id)
            .ToList();

        if (issuedCouponIds.Count == 0)
            return [];

        var offerIds = (await writeStore.Coupons
                .Where(c => issuedCouponIds.Contains(c.Id) && c.Email == email)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return eventWaitlists
            .Where(w => w.Coupons.Any(c => offerIds.Contains(c.Id) && c.Status == WaitlistCouponStatus.Issued))
            .Select(w => w.Id)
            .ToList();
    }
}
