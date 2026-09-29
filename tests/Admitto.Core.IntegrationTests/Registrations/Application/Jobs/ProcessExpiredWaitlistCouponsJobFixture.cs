using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.Jobs;

internal sealed class ProcessExpiredWaitlistCouponsJobFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
    public TimeZoneId TimeZone { get; } = TimeZoneId.From("UTC");

    private ProcessExpiredWaitlistCouponsJobFixture()
    {
    }

    /// <summary>
    /// Two waitlist entries, one issued coupon — room for a second notification after revocation.
    /// </summary>
    public static ProcessExpiredWaitlistCouponsJobFixture WithTwoEntriesOnePendingCoupon() => new();

    /// <summary>
    /// One waitlist entry, one issued coupon — no further entries after revocation.
    /// </summary>
    public static ProcessExpiredWaitlistCouponsJobFixture WithOneEntryOnePendingCoupon() => new();

    /// <summary>
    /// Three waitlist entries, two issued coupons — one more person waiting after both lapse.
    /// </summary>
    public static ProcessExpiredWaitlistCouponsJobFixture WithThreeEntriesTwoPendingCoupons() =>
        new() { CouponsToIssue = 2 };

    /// <summary>
    /// One VIP coupon promoted from the back of the queue, one more entry waiting behind it.
    /// </summary>
    public static ProcessExpiredWaitlistCouponsJobFixture WithOnePendingVipCoupon() =>
        new() { CouponsToIssue = 0, VipCouponsToIssue = 1 };

    /// <summary>
    /// One automatic coupon and one VIP coupon outstanding at the same time.
    /// </summary>
    public static ProcessExpiredWaitlistCouponsJobFixture WithOnePendingCouponAndOnePendingVipCoupon() =>
        new() { VipCouponsToIssue = 1 };

    /// <summary>
    /// Number of coupons issued (to the front-of-queue entries) during setup.
    /// </summary>
    public int CouponsToIssue { get; private init; } = 1;

    /// <summary>
    /// Number of VIP coupons issued during setup by promoting the entries at the back of the queue.
    /// </summary>
    public int VipCouponsToIssue { get; private init; }

    /// <summary>
    /// Seeds the database with a TicketedEvent, TicketCatalog in WaitlistMode, a Waitlist with
    /// <paramref name="activeEntries"/> entries, and then issues a coupon to the first entry using
    /// the real handler so the coupon row exists in the DB with a real <c>expires_at</c>.
    /// Issues <see cref="CouponsToIssue"/> coupons to the front-of-queue entries, then promotes the last
    /// <see cref="VipCouponsToIssue"/> entries as VIPs.
    /// </summary>
    /// <param name="activeEntriesAfterCoupon">
    /// Number of active entries that should remain in the waitlist AFTER the coupons are issued.
    /// Pass 1 to leave one more waiting person; pass 0 for an empty waitlist after the coupon.
    /// </param>
    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        int activeEntriesAfterCoupon,
        CancellationToken cancellationToken = default)
    {
        // Total entries = the ones that will receive a coupon + the remaining active ones.
        var totalEntries = CouponsToIssue + VipCouponsToIssue + activeEntriesAfterCoupon;
        // Always at least one seat, filled, so WaitlistMode is on even when only VIP coupons are issued.
        var capacity = Math.Max(CouponsToIssue, 1);

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("DevConf 2026"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZone);
            dbContext.TicketedEvents.Add(ticketedEvent);

            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(TicketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity: capacity,
                waitlistEnabled: true, claimWindowHours: 8);
            for (var i = 0; i < capacity; i++)
                catalog.Claim([TicketTypeId], ClaimMode.Public);   // fill to capacity → WaitlistMode activates
            dbContext.TicketCatalogs.Add(catalog);

            var waitlist = Waitlist.Create(EventId, TicketTypeId, TeamId);
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < totalEntries; i++)
                waitlist.AddEntry(EmailAddress.From($"attendee{i + 1}@example.com"), now.AddMinutes(i));

            dbContext.Waitlists.Add(waitlist);
        }, cancellationToken);

        // Issue coupons to the front entries using the real handler, so proper coupon rows are
        // persisted before we backdate expires_at via raw SQL.
        var context = environment.RegistrationsDatabase.Context;
        if (CouponsToIssue > 0)
        {
            var handler = new ProcessWaitlistNotificationsHandler(context, TimeProvider.System);

            await handler.HandleAsync(
                new ProcessWaitlistNotificationsCommand(EventId.Value, TeamId.Value, TicketTypeId.Value, FreedSlots: CouponsToIssue),
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        if (VipCouponsToIssue > 0)
        {
            var waitlist = await context.Waitlists.AsNoTracking()
                .FirstAsync(w => w.Id == TicketTypeId, cancellationToken);
            var vipEntryIds = waitlist.Entries
                .Where(e => e.Status == WaitlistEntryStatus.Active)
                .OrderByDescending(e => e.Position)
                .Take(VipCouponsToIssue)
                .Select(e => e.Id.Value)
                .ToList();

            var promoteHandler = new PromoteWaitlistEntryHandler(context, TimeProvider.System);
            foreach (var entryId in vipEntryIds)
            {
                await promoteHandler.HandleAsync(
                    new PromoteWaitlistEntryCommand(EventId.Value, TeamId.Value, TicketTypeId.Value, entryId),
                    cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Backdates the <c>expires_at</c> of all unredeemed, unrevoked waitlist coupons to be
    /// <paramref name="offsetFromNow"/> before now, bypassing the domain model.
    /// </summary>
    public async ValueTask BackdateCouponExpiryAsync(
        IntegrationTestEnvironment environment,
        TimeSpan offsetFromNow,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - offsetFromNow;

        await environment.RegistrationsDatabase.Context.Database.ExecuteSqlAsync(
            $"UPDATE registrations.coupons SET expires_at = {cutoff} WHERE source = {nameof(CouponSource.Waitlist)} AND redeemed_at IS NULL AND revoked_at IS NULL",
            cancellationToken);

        environment.RegistrationsDatabase.Context.ChangeTracker.Clear();
    }
}
