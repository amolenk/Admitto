using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist;
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
    /// Two waitlist entries, one issued coupon — room for a second notification after expiry.
    /// </summary>
    public static ProcessExpiredWaitlistCouponsJobFixture WithTwoEntriesOnePendingCoupon() => new();

    /// <summary>
    /// One waitlist entry, one issued coupon — no further entries after expiry.
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
    /// An automatic coupon for the front of the queue, then a VIP coupon for the next entry (position 2) made while
    /// sold out, with one more entry waiting behind them.
    /// </summary>
    public static ProcessExpiredWaitlistCouponsJobFixture WithPendingCouponAndVipCouponAtPositionTwo() =>
        new() { VipCouponsToIssue = 1, VipsFromFront = true };

    /// <summary>
    /// Number of coupons issued (to the front-of-queue entries) during setup.
    /// </summary>
    public int CouponsToIssue { get; private init; } = 1;

    /// <summary>
    /// Number of VIP coupons issued during setup by promoting the entries at the back of the queue.
    /// </summary>
    public int VipCouponsToIssue { get; private init; }

    /// <summary>
    /// Whether VIP coupons go to the front of the remaining queue instead of its back.
    /// </summary>
    public bool VipsFromFront { get; private init; }

    /// <summary>
    /// Seeds the database with a TicketedEvent, a sold-out TicketCatalog in WaitlistMode and a Waitlist, then
    /// frees <see cref="CouponsToIssue"/> seats and issues that many coupons to the front-of-queue entries using
    /// the real handler so the coupon rows exist in the DB with a real <c>expires_at</c>. Finally promotes
    /// <see cref="VipCouponsToIssue"/> entries (from the back of the queue, or its front if <see cref="VipsFromFront"/>)
    /// as VIPs while the ticket type is sold out; their offers take no hold, being admin tickets on top of capacity.
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
            catalog.AddTicketType(TicketTypeId, TicketTypeName.From("Conference Pass"), [], publicCapacity: capacity,
                waitlistEnabled: true, claimWindowHours: 8);
            var tickets = Enumerable.Range(0, capacity)
                .Select(_ => catalog.Claim([TicketTypeId], ClaimMode.Public))   // fill to capacity → WaitlistMode
                .ToList();
            foreach (var ticket in tickets.Take(CouponsToIssue))
                catalog.Release(ticket);   // seats for the coupons issued below
            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            var waitlist = Waitlist.Create(EventId, TicketTypeId, TeamId);
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < totalEntries; i++)
                waitlist.AddEntry(EmailAddress.From($"attendee{i + 1}@example.com"), now.AddMinutes(i), catalog);

            dbContext.Waitlists.Add(waitlist);
        }, cancellationToken);

        // Issue coupons to the front entries using the real handler, so proper coupon rows are
        // persisted before we backdate expires_at via raw SQL.
        var context = environment.RegistrationsDatabase.Context;
        if (CouponsToIssue > 0)
        {
            var handler = new ProcessWaitlistNotificationsHandler(context, TimeProvider.System);

            await handler.HandleAsync(
                new ProcessWaitlistNotificationsCommand(EventId.Value, TeamId.Value, TicketTypeId.Value),
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
        }

        if (VipCouponsToIssue > 0)
        {
            var waitlist = await context.Waitlists.AsNoTracking()
                .FirstAsync(w => w.Id == TicketTypeId, cancellationToken);
            var activeEntries = waitlist.Entries.Where(e => e.Status == WaitlistEntryStatus.Active);
            var vipEntryIds = (VipsFromFront
                    ? activeEntries.OrderBy(e => e.Position)
                    : activeEntries.OrderByDescending(e => e.Position))
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
    /// Has the organizer explicitly disable the ticket type's waitlist (keeping its capacity), applying the
    /// resulting <c>WaitlistDisabledDomainEvent</c> through the real handler: everyone still waiting is removed,
    /// outstanding coupons stay issued.
    /// </summary>
    public async ValueTask DisableWaitlistAsync(
        IntegrationTestEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var context = environment.RegistrationsDatabase.Context;
        var capacity = Math.Max(CouponsToIssue, 1);

        await new UpdateTicketTypeHandler(context).HandleAsync(
            new UpdateTicketTypeCommand(
                EventId.Value, TeamId.Value, TicketTypeId.Value, Name: null, PublicCapacity: capacity, WaitlistEnabled: false),
            cancellationToken);
        await new DisableWaitlistHandler(context, TimeProvider.System).HandleAsync(
            new DisableWaitlistCommand(EventId.Value, TeamId.Value, TicketTypeId.Value, FreedSlots: 0),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Archives the ticketed event and its catalog, as happens once an organizer archives a finished event.
    /// </summary>
    public async ValueTask ArchiveEventAsync(
        IntegrationTestEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var context = environment.RegistrationsDatabase.Context;

        var ticketedEvent = await context.TicketedEvents
            .SingleAsync(e => e.Id == EventId && e.TeamId == TeamId, cancellationToken);
        ticketedEvent.Archive();

        var catalog = await context.TicketCatalogs
            .SingleAsync(c => c.Id == EventId && c.TeamId == TeamId, cancellationToken);
        catalog.MarkEventArchived();

        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Backdates the expiry of the outstanding waitlist coupons — all of them, or only the one sent to
    /// <paramref name="recipient"/> — to be <paramref name="offsetFromNow"/> before now, bypassing the domain model:
    /// both the coupon rows and the issued coupons tracked in the waitlist's <c>waitlist_coupons</c> JSON (which is
    /// what the expiry job looks at).
    /// </summary>
    public async ValueTask BackdateCouponExpiryAsync(
        IntegrationTestEnvironment environment,
        TimeSpan offsetFromNow,
        CancellationToken cancellationToken = default,
        string? recipient = null)
    {
        var cutoff = DateTimeOffset.UtcNow - offsetFromNow;

        var database = environment.RegistrationsDatabase.Context.Database;

        var couponIds = await environment.RegistrationsDatabase.Context.Coupons
            .AsNoTracking()
            .Where(c => c.Source == CouponSource.Waitlist && c.RedeemedAt == null)
            .Select(c => new { c.Id, c.Email })
            .ToListAsync(cancellationToken);
        var targetIds = couponIds
            .Where(c => recipient is null || c.Email.Value == recipient)
            .Select(c => c.Id.Value.ToString())
            .ToArray();

        await database.ExecuteSqlAsync(
            $"UPDATE registrations.coupons SET expires_at = {cutoff} WHERE id::text = ANY({targetIds})",
            cancellationToken);

        await database.ExecuteSqlAsync(
            $$"""
             UPDATE registrations.waitlists
             SET waitlist_coupons = (
                 SELECT jsonb_agg(
                     CASE WHEN c->>'status' = {{nameof(WaitlistCouponStatus.Issued)}} AND c->>'id' = ANY({{targetIds}})
                          THEN jsonb_set(c, '{expires_at}', to_jsonb({{cutoff}}))
                          ELSE c
                     END)
                 FROM jsonb_array_elements(waitlist_coupons) AS c)
             WHERE ticket_type_id = {{TicketTypeId.Value}}
             """,
            cancellationToken);

        environment.RegistrationsDatabase.Context.ChangeTracker.Clear();
    }
}
