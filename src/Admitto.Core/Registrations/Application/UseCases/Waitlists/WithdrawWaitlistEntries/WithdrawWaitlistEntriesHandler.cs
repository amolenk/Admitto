using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries;

/// <summary>
/// Withdraws an attendee from every waitlist selection they still hold, across all ticket types for the event —
/// whether still actively queued or holding an outstanding, unredeemed offer — so a cancelled attendee can never
/// subsequently receive or redeem a promotion. An outstanding offer's coupon is silently expired (no expired-offer
/// email; the attendee already knows their registration was cancelled) and an automatic offer's held seat is
/// released, same as any other withdrawal. Each withdrawn entry leaves the ticket catalog's queued count in the
/// same unit of work.
/// </summary>
internal sealed class WithdrawWaitlistEntriesHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<WithdrawWaitlistEntriesCommand>
{
    public async ValueTask HandleAsync(
        WithdrawWaitlistEntriesCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var email = EmailAddress.From(command.Email);

        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == eventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);

        var withdrawnWaitlists = waitlists
            .Where(w => w.HasActiveEntry(email) || w.HasOfferedEntry(email))
            .ToList();
        if (withdrawnWaitlists.Count == 0)
            return;

        var catalog = await writeStore.TicketCatalogs.GetAsync(
            c => c.Id == eventId && c.TeamId == teamId,
            cancellationToken);

        var withdrawnCouponIds = new List<CouponId>();
        foreach (var waitlist in withdrawnWaitlists)
        {
            if (waitlist.RemoveEntry(email, catalog) is { } couponId)
                withdrawnCouponIds.Add(couponId);
        }

        if (withdrawnCouponIds.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        var offers = await writeStore.Coupons
            .Where(c => withdrawnCouponIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        foreach (var offer in offers)
            offer.Expire(now);
    }
}
