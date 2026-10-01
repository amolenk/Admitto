using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;

internal sealed class UpdatePartnerRegistrationHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<UpdatePartnerRegistrationCommand>
{
    public async ValueTask HandleAsync(
        UpdatePartnerRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var registrationId = RegistrationId.From(command.RegistrationId);
        var firstName = FirstName.From(command.FirstName);
        var lastName = LastName.From(command.LastName);
        var registerTicketTypeIds = command.RegisterTicketTypeIds.Select(TicketTypeId.From).ToList();
        var waitlistTicketTypeIds = command.WaitlistTicketTypeIds.Select(TicketTypeId.From).ToList();

        RegisterAttendeeSelfServiceHandler.EnsureNoDuplicateRequestedActions(registerTicketTypeIds, waitlistTicketTypeIds);

        var registration = await writeStore.Registrations.GetAsync(
            r => r.Id == registrationId && r.EventId == eventId && r.TeamId == teamId,
            cancellationToken);

        if (registration.Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        var ticketedEvent = await writeStore.TicketedEvents
            .GetAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);

        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(TicketedEvent.Errors.EventNotActive);

        var now = timeProvider.GetUtcNow();
        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        var catalog = await writeStore.TicketCatalogs
            .GetAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);

        catalog.ValidateSelection(registerTicketTypeIds);

        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == eventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);

        var currentConfirmedIds = registration.Tickets.Select(t => t.Id).ToHashSet();
        var currentWaitlistIds = waitlists
            .Where(w => w.HasActiveEntry(registration.Email))
            .Select(w => w.Id)
            .ToHashSet();

        var registerSet = registerTicketTypeIds.ToHashSet();
        var waitlistSet = waitlistTicketTypeIds.ToHashSet();

        var toConfirm = registerSet.Except(currentConfirmedIds).ToList();
        var toReleaseConfirmed = currentConfirmedIds.Except(registerSet).ToList();
        var toWaitlistJoin = waitlistSet.Except(currentWaitlistIds).ToList();
        var toWaitlistLeave = currentWaitlistIds.Except(waitlistSet).ToList();

        // Any coupon, whatever its source, can back the newly confirmed ticket types. The ones it grants bypass
        // the self-service ticket-state classification and capacity gate, like every coupon claim. Tickets the
        // registration already holds are not granted by the coupon, so they cannot satisfy its redemption.
        Coupon? coupon = null;
        IReadOnlyList<TicketTypeId> couponGrantedIds = [];
        if (command.CouponCode is { } couponCode)
        {
            coupon = await writeStore.Coupons.GetAsync(
                c => c.EventId == eventId && c.TeamId == teamId && c.Code == CouponCode.From(couponCode),
                cancellationToken);

            couponGrantedIds = coupon.Redeem(registration.Email, toConfirm, now);
        }

        var toConfirmPublicly = toConfirm.Except(couponGrantedIds).ToList();

        // A coupon that bypasses the registration window (e.g. a waitlist offer issued before registration closed)
        // can still be claimed after close, but only for what it grants: any other ticket or waitlist change in the
        // same request still needs the window to be open.
        var hasOtherChanges = toConfirmPublicly.Count > 0
                              || toReleaseConfirmed.Count > 0
                              || toWaitlistJoin.Count > 0
                              || toWaitlistLeave.Count > 0;
        if (!RegisterAttendeeWithCouponHandler.WindowBypassApplies(coupon, hasOtherChanges))
            ticketedEvent.EnsureRegistrationOpen(now);

        RegisterAttendeeSelfServiceHandler.EnsureRequestedTicketStatesMatch(catalog, toConfirmPublicly, toWaitlistJoin);
        RegisterAttendeeSelfServiceHandler.ValidateWaitlistRequests(catalog, toWaitlistJoin);

        var claimedTickets = catalog.Claim(toConfirmPublicly, ClaimMode.Public);
        var couponClaimedTickets = coupon is null
            ? []
            : catalog.ClaimWithCoupon(couponGrantedIds, coupon);

        var releasedSnapshots = registration.Tickets.Where(t => toReleaseConfirmed.Contains(t.Id)).ToList();
        catalog.Release(releasedSnapshots);

        // Newly claimed tickets keep the ClaimMode they were claimed under; tickets that were
        // already on the registration keep their originally recorded mode so a later release
        // still credits the correct pool.
        var existingTicketsById = registration.Tickets.ToDictionary(t => t.Id);
        var claimedTicketsById = claimedTickets.Concat(couponClaimedTickets).ToDictionary(t => t.Id);
        var newTickets = registerTicketTypeIds
            .Select(id =>
            {
                if (claimedTicketsById.TryGetValue(id, out var claimed))
                    return claimed;
                if (existingTicketsById.TryGetValue(id, out var existing))
                    return existing;

                var ticketType = catalog.GetTicketType(id);
                var timeSlots = ticketType?.TimeSlots ?? [];
                var name = ticketType?.Name ?? TicketTypeName.From(id.Value.ToString());
                return new TicketTypeSnapshot(id, name, timeSlots);
            })
            .ToList();

        registration.ReplaceAttendeeEditableState(
            firstName,
            lastName,
            additionalDetails,
            newTickets,
            catalog.DescribeTicketTypes(currentWaitlistIds),
            catalog.DescribeTicketTypes(waitlistTicketTypeIds),
            now);

        var waitlistsById = waitlists.ToDictionary(w => w.Id);

        foreach (var ticketTypeId in toWaitlistLeave)
        {
            if (waitlistsById.TryGetValue(ticketTypeId, out var waitlist))
                waitlist.RemoveEntry(registration.Email, catalog);
        }

        foreach (var ticketTypeId in toWaitlistJoin)
        {
            if (!waitlistsById.TryGetValue(ticketTypeId, out var waitlist))
            {
                waitlist = Waitlist.Create(eventId, ticketTypeId, teamId);
                await writeStore.Waitlists.AddAsync(waitlist, cancellationToken);
                waitlistsById[ticketTypeId] = waitlist;
            }

            waitlist.AddEntry(registration.Email, now, catalog);
        }

        if (coupon is not null)
        {
            await RegisterAttendeeWithCouponHandler.ApplyRedemptionToWaitlistsAsync(
                writeStore, waitlists, catalog, coupon, registration.Email, couponGrantedIds,
                now, cancellationToken);
        }
    }

    internal static class Errors
    {
        public static readonly Error RegistrationIsCancelled = new(
            "registration.is_cancelled",
            "Registration is cancelled.",
            Type: ErrorType.Conflict);
    }
}
