using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;

internal sealed class RegisterAttendeeWithCouponHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterAttendeeWithCouponCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        RegisterAttendeeWithCouponCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var email = EmailAddress.From(command.Email);
        var firstName = FirstName.From(command.FirstName);
        var lastName = LastName.From(command.LastName);
        var ticketTypeIds = command.TicketTypeIds.Select(TicketTypeId.From).ToList();

        var coupon = await writeStore.Coupons.GetAsync(
            c => c.EventId == eventId && c.TeamId == teamId && c.Code == CouponCode.From(command.CouponCode),
            cancellationToken);

        var ticketedEvent = await writeStore.TicketedEvents
            .GetAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);

        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        var now = timeProvider.GetUtcNow();
        // The coupon is the only claim source for this registration, so it cannot carry ticket types it doesn't cover.
        coupon.EnsureAllowsAll(ticketTypeIds);
        var couponGrantedIds = coupon.Redeem(email, ticketTypeIds, now);

        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        if (!coupon.BypassRegistrationWindow)
            ticketedEvent.EnsureRegistrationOpen(now);

        var existingRegistration = await writeStore.Registrations
            .SingleOrDefaultAsync(
                r => r.EventId == eventId && r.TeamId == teamId && r.Email == email,
                cancellationToken);

        if (existingRegistration?.Status == RegistrationStatus.Registered)
            throw new BusinessRuleViolationException(AlreadyExistsError.Create<Registration>());

        var catalog = await writeStore.TicketCatalogs
            .GetAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);

        var tickets = catalog.ClaimWithCoupon(ticketTypeIds, coupon);

        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == eventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);
        ApplyRedemptionToWaitlists(waitlists, catalog, coupon, email, couponGrantedIds);

        // A waitlisted attendee claiming their offer keeps their other waitlist entries.
        var waitlistedTickets = DescribeActiveWaitlistEntries(waitlists, catalog, email);

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
                waitlistedTickets);
            await writeStore.Registrations.AddAsync(registration, cancellationToken);
        }
        else
        {
            registration = existingRegistration;
            registration.Reset(firstName, lastName, tickets, additionalDetails, now, waitlistedTickets);
        }

        return registration.Id.Value;
    }

    /// <summary>
    /// Describes the ticket types the email currently holds an active waitlist entry for.
    /// </summary>
    internal static IReadOnlyList<TicketTypeSnapshot> DescribeActiveWaitlistEntries(
        IEnumerable<Waitlist> eventWaitlists,
        TicketCatalog catalog,
        EmailAddress email) =>
        catalog.DescribeTicketTypes(eventWaitlists.Where(w => w.HasActiveEntry(email)).Select(w => w.Id));

    /// <summary>
    /// Redemption-time waitlist cleanup shared by every coupon redemption path: for each ticket type the
    /// redemption actually granted, removes the redeeming email's active waitlist entry and settles the
    /// coupon on the waitlist that issued it — uniformly, whatever the coupon's source. Removed entries leave the
    /// <paramref name="catalog"/>'s queued count.
    /// </summary>
    internal static void ApplyRedemptionToWaitlists(
        IEnumerable<Waitlist> eventWaitlists,
        TicketCatalog catalog,
        Coupon coupon,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> couponGrantedIds)
    {
        foreach (var waitlist in eventWaitlists.Where(w => couponGrantedIds.Contains(w.Id)))
        {
            waitlist.ApplyCouponRedemption(coupon.Id, email, catalog);
        }
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "registration.event_not_active",
            "Cannot register for a cancelled or archived event.",
            Type: ErrorType.Validation);
    }
}
