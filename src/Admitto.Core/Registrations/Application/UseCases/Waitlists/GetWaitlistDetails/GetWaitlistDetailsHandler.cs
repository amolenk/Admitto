using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.GetWaitlistDetails;

internal sealed class GetWaitlistDetailsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetWaitlistDetailsQuery, WaitlistDetailsDto?>
{
    public async ValueTask<WaitlistDetailsDto?> HandleAsync(
        GetWaitlistDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var ticketTypeId = TicketTypeId.From(query.TicketTypeId);
        var ticketedEventId = TicketedEventId.From(query.EventId);
        var teamId = TeamId.From(query.TeamId);

        var waitlist = await writeStore.Waitlists
            .AsNoTracking()
            .Include(w => w.Entries)
            .Include(w => w.Coupons)
            .FirstOrDefaultAsync(w => w.Id == ticketTypeId && w.EventId == ticketedEventId && w.TeamId == teamId, cancellationToken);

        if (waitlist is null)
            return null;

        var activeEntries = waitlist.Entries
            .Where(e => e.Status == WaitlistEntryStatus.Active)
            .OrderBy(e => e.Position)
            .ToList();

        var issuedCoupons = waitlist.Coupons
            .Where(c => c.Status == WaitlistCouponStatus.Issued)
            .ToList();

        var issuedCouponIds = issuedCoupons.Select(c => c.Id).ToHashSet();

        var coupons = await writeStore.Coupons
            .AsNoTracking()
            .Where(c => issuedCouponIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var couponById = coupons.ToDictionary(c => c.Id);

        var today = DateTimeOffset.UtcNow.Date;

        var activeEntryRows = activeEntries
            .Select(e => new WaitlistEntryRow(
                e.Id.Value,
                e.Position,
                MaskEmail(e.Email.Value),
                e.AddedAt))
            .ToList();

        var pendingRows = issuedCoupons
            .Where(wc => couponById.ContainsKey(wc.Id))
            .Select(wc => new PendingNotificationRow(
                wc.Id.Value,
                MaskEmail(couponById[wc.Id].Email.Value),
                couponById[wc.Id].ExpiresAt))
            .ToList();

        var sentToday = issuedCoupons.Count(c => c.IssuedAt.UtcDateTime.Date == today);

        var stats = new WaitlistStats(
            TotalWaiting: activeEntries.Count,
            TotalPending: issuedCoupons.Count,
            SentToday: sentToday);

        return new WaitlistDetailsDto(activeEntryRows, pendingRows, stats);
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return email;

        var local = email[..atIndex];
        var domain = email[atIndex..];

        var visibleChars = Math.Min(3, local.Length);
        return local[..visibleChars] + "***" + domain;
    }
}
