using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;
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

        // 6. Compute delta: toRelease = current ∖ new, toClaim = new ∖ current.
        var currentIds = registration.Tickets.Select(t => t.Id.Value).ToHashSet();
        var newIdsSet = command.TicketTypeIds.ToHashSet();

        var toRelease = registration.Tickets.Where(t => !newIdsSet.Contains(t.Id.Value)).ToList();
        var toClaim = newTicketTypeIds.Where(id => !currentIds.Contains(id.Value)).ToList();

        // Any coupon, whatever its source, can back the newly added ticket types; tickets the registration
        // already holds are not granted by the coupon, so they cannot satisfy its redemption.
        Coupon? coupon = null;
        IReadOnlyList<TicketTypeId> couponGrantedIds = [];
        if (command.CouponCode is { } couponCode)
        {
            coupon = await writeStore.Coupons.GetAsync(
                c => c.EventId == eventId && c.TeamId == teamId && c.Code == CouponCode.From(couponCode),
                cancellationToken);

            couponGrantedIds = coupon.Redeem(registration.Email, toClaim, timeProvider.GetUtcNow());
        }

        // 7. Release freed capacity.
        catalog.Release(toRelease);

        // 8. Claim added capacity. Ticket types granted by a coupon are claimed under the coupon's pool; tickets an
        // admin adds are admin tickets on top of public capacity.
        toClaim = toClaim.Except(couponGrantedIds).ToList();

        var claimMode = command.Mode == ChangeMode.SelfService ? ClaimMode.Public : ClaimMode.Admin;
        var claimedTickets = catalog.Claim(toClaim, claimMode);
        var couponClaimedTickets = coupon is null
            ? []
            : catalog.ClaimWithCoupon(couponGrantedIds, coupon);

        // 9. Build new ticket snapshots. Newly claimed tickets keep the ClaimMode they were
        // claimed under (see step 8); tickets that were already on the registration keep their
        // originally recorded mode so a later release still credits the correct pool.
        var existingTicketsById = registration.Tickets.ToDictionary(t => t.Id);
        var claimedTicketsById = claimedTickets.Concat(couponClaimedTickets).ToDictionary(t => t.Id);
        var newTickets = newTicketTypeIds
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

        // 10. Apply the change to the registration. Waitlist entries are untouched by this change,
        // except that a coupon redemption clears the entries for the ticket types it granted.
        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == eventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);
        var currentWaitlistIds = waitlists
            .Where(w => w.HasActiveEntry(registration.Email))
            .Select(w => w.Id)
            .ToList();
        registration.ChangeTickets(
            newTickets,
            catalog.DescribeTicketTypes(currentWaitlistIds),
            catalog.DescribeTicketTypes(currentWaitlistIds.Except(couponGrantedIds)),
            timeProvider.GetUtcNow());

        if (coupon is null)
            return;

        RegisterAttendeeWithCouponHandler.ApplyRedemptionToWaitlists(
            waitlists, catalog, coupon, registration.Email, couponGrantedIds);
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
    }
}
