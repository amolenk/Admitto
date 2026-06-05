using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;

/// <summary>
/// Issues waitlist coupons to the top-ranked attendees when capacity becomes available.
/// Creates one <see cref="Coupon"/> per freed slot (up to the number of active waitlist entries),
/// tracks each in the <see cref="Domain.Entities.Waitlist"/>, and emits
/// <see cref="Domain.DomainEvents.WaitlistCouponIssuedDomainEvent"/> per recipient so the email
/// notification is published via the outbox.
/// </summary>
internal sealed class ProcessWaitlistNotificationsHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<ProcessWaitlistNotificationsCommand>
{
    public async ValueTask HandleAsync(
        ProcessWaitlistNotificationsCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(eventId, cancellationToken);
        var catalog = await writeStore.TicketCatalogs.GetAsync(eventId, cancellationToken);

        var ticketType = catalog.GetTicketType(ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistEnabled || !ticketType.WaitlistMode)
            return;

        var waitlist = await writeStore.Waitlists.GetAsync(ticketTypeId, cancellationToken);

        // var activeEntryCount = waitlist.Entries.Count(e => e.Status == WaitlistEntryStatus.Active);
        var activeEntryCount = waitlist.ActiveEntryCount;
        var slotsToProcess = Math.Min(command.FreedSlots, activeEntryCount);

        if (slotsToProcess <= 0 && waitlist.IssuedCouponCount == 0)
        {
            // Waitlist exhausted (no active entries, no outstanding coupons) — lift WaitlistMode
            catalog.ForceDeactivateWaitlistMode(ticketTypeId);
            return;
        }

        var utcNow = timeProvider.GetUtcNow();
        // var couponExpiresAt = WaitlistClaimWindowCalculator.ComputeExpiresAt(
        //     utcNow,
        //     ticketedEvent.TimeZone,
        //     ticketedEvent.QuietHoursStart,
        //     ticketedEvent.QuietHoursEnd,
        //     ticketType.ClaimWindowHours);


        for (var i = 0; i < slotsToProcess; i++)
        {
            var coupon = waitlist.IssueNextCoupon(ticketedEvent, ticketType, utcNow);

            // var nextEntry = waitlist.Entries
            //     .Where(e => e.Status == WaitlistEntryStatus.Active)
            //     .MinBy(e => e.Position);

            if (coupon is null)
                break;

            // var coupon = Coupon.Create(
            //     eventId,
            //     ticketedEvent.TeamId,
            //     nextEntry.Email,
            //     [ticketTypeId],
            //     couponExpiresAt,
            //     bypassRegistrationWindow: true,
            //     ticketTypeInfos,
            //     utcNow,
            //     CouponSource.Waitlist);

            await writeStore.Coupons.AddAsync(coupon, cancellationToken);

            // var coupon = waitlist.IssueNextCoupon(coupon.Id, coupon.Code, ticketType.Name.Value, couponExpiresAt, utcNow);
        }

        var remainingActiveEntries = activeEntryCount - slotsToProcess;
        catalog.ReEvaluateWaitlistMode(ticketTypeId, remainingActiveEntries, waitlist.IssuedCouponCount);
    }
}
