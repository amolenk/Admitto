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

        var catalog = await writeStore.TicketCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == ticketedEventId && tc.TeamId == teamId, cancellationToken);

        var ticketType = catalog?.TicketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);

        if (ticketType is null)
            return null;

        if (!ticketType.WaitlistEnabled)
        {
            return new WaitlistDetailsDto(
                WaitlistEnabled: false,
                ActiveEntries: [],
                PendingNotifications: [],
                Stats: new WaitlistStats(TotalWaiting: 0, TotalPending: 0, SentToday: 0));
        }

        var waitlist = await writeStore.Waitlists
            .AsNoTracking()
            .Include(w => w.Entries)
            .Include(w => w.Coupons)
            .FirstOrDefaultAsync(w => w.Id == ticketTypeId && w.EventId == ticketedEventId && w.TeamId == teamId, cancellationToken);

        if (waitlist is null)
        {
            return new WaitlistDetailsDto(
                WaitlistEnabled: true,
                ActiveEntries: [],
                PendingNotifications: [],
                Stats: new WaitlistStats(TotalWaiting: 0, TotalPending: 0, SentToday: 0));
        }

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

        var registrationIds = activeEntries.Select(e => e.RegistrationId).ToHashSet();
        var couponEmails = coupons.Select(c => c.Email).ToHashSet();

        var registrationsById = await writeStore.Registrations
            .AsNoTracking()
            .Where(r => registrationIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Email, r.FirstName, r.LastName })
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var registrationsByEmail = await writeStore.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == ticketedEventId && r.TeamId == teamId && couponEmails.Contains(r.Email))
            .Select(r => new { r.Id, r.Email, r.FirstName, r.LastName })
            .ToDictionaryAsync(r => r.Email, cancellationToken);

        var today = DateTimeOffset.UtcNow.Date;

        var activeEntryRows = activeEntries
            .Select(e =>
            {
                registrationsById.TryGetValue(e.RegistrationId, out var registration);
                return new WaitlistEntryRow(
                    e.Id.Value,
                    e.Position,
                    e.RegistrationId.Value,
                    registration?.Email.Value ?? e.Email.Value,
                    registration?.FirstName.Value ?? string.Empty,
                    registration?.LastName.Value ?? string.Empty,
                    e.AddedAt);
            })
            .ToList();

        var pendingRows = issuedCoupons
            .Where(wc => couponById.ContainsKey(wc.Id))
            .Select(wc =>
            {
                var coupon = couponById[wc.Id];
                registrationsByEmail.TryGetValue(coupon.Email, out var registration);
                return new PendingNotificationRow(
                    wc.Id.Value,
                    registration?.Id.Value ?? Guid.Empty,
                    coupon.Email.Value,
                    registration?.FirstName.Value ?? string.Empty,
                    registration?.LastName.Value ?? string.Empty,
                    coupon.ExpiresAt);
            })
            .ToList();

        var sentToday = issuedCoupons.Count(c => c.IssuedAt.UtcDateTime.Date == today);

        var stats = new WaitlistStats(
            TotalWaiting: activeEntries.Count,
            TotalPending: issuedCoupons.Count,
            SentToday: sentToday);

        return new WaitlistDetailsDto(WaitlistEnabled: true, activeEntryRows, pendingRows, stats);
    }
}
