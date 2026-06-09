using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets;

internal sealed class ChangeAttendeeTicketsHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<ChangeAttendeeTicketsCommand>
{
    public async ValueTask HandleAsync(
        ChangeAttendeeTicketsCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);
        RegistrationId registrationId = RegistrationId.From(command.RegistrationId);

        // 1. Load registration; reject if not found.
        var registration = await writeStore.Registrations.GetAsync(
                 r => r.Id == registrationId && r.EventId == eventId && r.TeamId == teamId,
                 cancellationToken);

        // 2. Reject cancelled registrations.
        if (registration.Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        // 3. For self-service, also enforce registration window.
        if (command.Mode == ChangeMode.SelfService)
        {
            var ticketedEvent = await writeStore.TicketedEvents
                .GetAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);

            var policy = ticketedEvent.RegistrationPolicy;
            var currentTime = timeProvider.GetUtcNow();
            if (policy is null || currentTime < policy.OpensAt || currentTime >= policy.ClosesAt)
                throw new BusinessRuleViolationException(Errors.RegistrationWindowClosed);
        }

        // 4. Load catalog.
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);

        if (catalog is null)
            throw new BusinessRuleViolationException(Errors.NoTicketTypesConfigured);

        // 5. Validate the full new selection (duplicates, unknown, cancelled, time slot conflicts).
        var newTicketTypeIds = command.TicketTypeIds.Select(TicketTypeId.From).ToList();
        catalog.ValidateSelection(newTicketTypeIds);

        Coupon? waitlistCoupon = null;
        TicketTypeId? couponTicketTypeId = null;
        if (command.WaitlistCouponCode is { } waitlistCouponCode)
        {
            waitlistCoupon = await writeStore.Coupons.GetAsync(
                c => c.EventId == eventId && c.TeamId == teamId && c.Code == CouponCode.From(waitlistCouponCode),
                cancellationToken);

            if (waitlistCoupon.Source != CouponSource.Waitlist)
                throw new BusinessRuleViolationException(Errors.WaitlistCouponRequired);

            if (waitlistCoupon.AllowedTicketTypeIds.Count != 1)
                throw new BusinessRuleViolationException(Errors.WaitlistCouponRequired);

            couponTicketTypeId = waitlistCoupon.AllowedTicketTypeIds[0];
            if (!newTicketTypeIds.Contains(couponTicketTypeId.Value))
                throw new BusinessRuleViolationException(Errors.WaitlistCouponTicketMissing(couponTicketTypeId.Value));
        }

        // 6. Compute delta: toRelease = current ∖ new, toClaim = new ∖ current.
        var currentIds = registration.Tickets.Select(t => t.Id.Value).ToHashSet();
        var newIdsSet = command.TicketTypeIds.ToHashSet();

        var toRelease = currentIds.Except(newIdsSet).Select(TicketTypeId.From).ToList();
        var toClaim = newTicketTypeIds.Where(id => !currentIds.Contains(id.Value)).ToList();

        // 7. Release freed capacity.
        catalog.Release(toRelease);

        // 8. Claim added capacity. A waitlist coupon grants only its offered ticket.
        var couponBackedClaim = couponTicketTypeId is { } offeredTicketTypeId
            && toClaim.Remove(offeredTicketTypeId);

        catalog.Claim(toClaim, enforce: command.Mode == ChangeMode.SelfService);
        if (couponBackedClaim)
            catalog.Claim([couponTicketTypeId!.Value], enforce: false);

        // 9. Build new ticket snapshots.
        var newTickets = newTicketTypeIds
            .Select(id =>
            {
                var ticketType = catalog.GetTicketType(id);
                var timeSlots = ticketType?.TimeSlots ?? [];
                var name = ticketType?.Name ?? TicketTypeName.From(id.Value.ToString());
                return new TicketTypeSnapshot(id, name, timeSlots);
            })
            .ToList();

        // 10. Apply the change to the registration.
        registration.ChangeTickets(newTickets, timeProvider.GetUtcNow());

        if (waitlistCoupon is null || couponTicketTypeId is null)
            return;

        var now = timeProvider.GetUtcNow();
        waitlistCoupon.Redeem(registration.Email, [couponTicketTypeId.Value], now);

        var waitlist = await writeStore.Waitlists.GetAsync(
            w => w.EventId == eventId && w.TeamId == teamId && w.Id == couponTicketTypeId.Value,
            cancellationToken);
        waitlist.RedeemCoupon(waitlistCoupon.Id);
    }

    internal static class Errors
    {
        public static readonly Error RegistrationWindowClosed = new(
            "change_tickets.registration_window_closed",
            "The registration window is not open for this event.",
            Type: ErrorType.Validation);

        public static readonly Error RegistrationIsCancelled = new(
            "registration.is_cancelled",
            "Registration is cancelled.",
            Type: ErrorType.Conflict);

        public static readonly Error NoTicketTypesConfigured = new(
            "change_tickets.no_ticket_types",
            "No ticket types have been configured for this event.",
            Type: ErrorType.Validation);

        public static readonly Error WaitlistCouponRequired = new(
            "change_tickets.waitlist_coupon_required",
            "The supplied coupon is not a waitlist coupon.",
            Type: ErrorType.Validation);

        public static Error WaitlistCouponTicketMissing(TicketTypeId ticketTypeId) => new(
            "change_tickets.waitlist_coupon_ticket_missing",
            "The final ticket selection must include the waitlist coupon's offered ticket type.",
            Type: ErrorType.Validation,
            Details: new Dictionary<string, object?> { ["ticketTypeId"] = ticketTypeId.Value });
    }
}
