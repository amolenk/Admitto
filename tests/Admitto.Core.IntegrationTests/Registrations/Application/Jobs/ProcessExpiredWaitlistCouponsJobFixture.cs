using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.ProcessWaitlistNotifications;
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
    /// Seeds the database with a TicketedEvent, TicketCatalog in WaitlistMode, a Waitlist with
    /// <paramref name="activeEntries"/> entries, and then issues a coupon to the first entry using
    /// the real handler so the coupon row exists in the DB with a real <c>expires_at</c>.
    /// </summary>
    /// <param name="activeEntriesAfterCoupon">
    /// Number of active entries that should remain in the waitlist AFTER the first coupon is issued.
    /// Pass 1 to leave one more waiting person; pass 0 for an empty waitlist after the coupon.
    /// </param>
    public async ValueTask SetupAsync(
        IntegrationTestEnvironment environment,
        int activeEntriesAfterCoupon,
        CancellationToken cancellationToken = default)
    {
        // Total entries = the one that will receive the coupon + the remaining active ones.
        var totalEntries = 1 + activeEntriesAfterCoupon;

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

            var catalog = TicketCatalog.Create(EventId);
            catalog.AddTicketType(TicketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity: 1,
                waitlistEnabled: true, claimWindowHours: 8);
            catalog.Claim([TicketTypeId], enforce: true);   // fill to capacity → WaitlistMode activates
            dbContext.TicketCatalogs.Add(catalog);

            var waitlist = Waitlist.Create(EventId, TicketTypeId, TeamId);
            var now = DateTimeOffset.UtcNow;
            for (var i = 0; i < totalEntries; i++)
                waitlist.AddEntry(EmailAddress.From($"attendee{i + 1}@example.com"), now.AddMinutes(i));

            dbContext.Waitlists.Add(waitlist);
        }, cancellationToken);

        // Issue a coupon to the first entry using the real handler, so a proper coupon row is
        // persisted before we backdate expires_at via raw SQL.
        var handler = new ProcessWaitlistNotificationsHandler(
            environment.RegistrationsDatabase.Context, TimeProvider.System);

        await handler.HandleAsync(
            new ProcessWaitlistNotificationsCommand(EventId.Value, TicketTypeId.Value, FreedSlots: 1),
            cancellationToken);

        await environment.RegistrationsDatabase.Context.SaveChangesAsync(cancellationToken);
        environment.RegistrationsDatabase.Context.ChangeTracker.Clear();
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
            $"UPDATE registrations.coupons SET expires_at = {cutoff} WHERE source = 1 AND redeemed_at IS NULL AND revoked_at IS NULL",
            cancellationToken);

        environment.RegistrationsDatabase.Context.ChangeTracker.Clear();
    }
}
