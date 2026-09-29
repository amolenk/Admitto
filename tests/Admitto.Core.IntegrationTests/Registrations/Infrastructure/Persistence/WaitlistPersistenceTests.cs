using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Infrastructure.Persistence;

[TestClass]
public sealed class WaitlistPersistenceTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a stored waitlist coupon written before coupon origins were tracked
    // When the waitlist is loaded
    // Then the coupon reads back as automatically issued
    [TestMethod]
    public async ValueTask Load_CouponJsonWithoutOrigin_ReadsAsAutomatic()
    {
        // Arrange — persist a VIP coupon, then strip the origin key from the stored JSON
        var (waitlist, ticketedEvent, ticketType) = CreateWaitlistWithOneEntry();
        waitlist.IssueCouponToEntry(waitlist.Entries.Single().Id, ticketedEvent, ticketType, DateTimeOffset.UtcNow);
        var ticketTypeId = waitlist.Id;

        await Environment.RegistrationsDatabase.SeedAsync(
            dbContext => dbContext.Waitlists.Add(waitlist), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.Context.Database.ExecuteSqlAsync(
            $"""
             UPDATE registrations.waitlists
             SET waitlist_coupons = (SELECT jsonb_agg(c - 'origin') FROM jsonb_array_elements(waitlist_coupons) c)
             WHERE ticket_type_id = {ticketTypeId.Value}
             """,
            testContext.CancellationToken);
        Environment.RegistrationsDatabase.Context.ChangeTracker.Clear();

        // Act & Assert
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var loaded = await ctx.Waitlists.SingleAsync(w => w.Id == ticketTypeId, testContext.CancellationToken);
            loaded.Coupons.ShouldHaveSingleItem().Origin.ShouldBe(WaitlistCouponOrigin.Automatic);
        });
    }

    // Given a waitlist that issued a coupon
    // When the waitlist is saved and loaded again
    // Then the tracked coupon keeps the coupon's expiry
    [TestMethod]
    public async ValueTask Load_IssuedCoupon_RoundTripsExpiresAt()
    {
        // Arrange
        var (waitlist, ticketedEvent, ticketType) = CreateWaitlistWithOneEntry();
        var coupon = waitlist.IssueNextCoupon(ticketedEvent, ticketType, DateTimeOffset.UtcNow)!;
        var ticketTypeId = waitlist.Id;

        await Environment.RegistrationsDatabase.SeedAsync(
            dbContext => dbContext.Waitlists.Add(waitlist), testContext.CancellationToken);

        // Act & Assert — Postgres stores microseconds, so compare at that precision
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var loaded = await ctx.Waitlists.SingleAsync(w => w.Id == ticketTypeId, testContext.CancellationToken);
            loaded.Coupons.ShouldHaveSingleItem().ExpiresAt
                .ShouldBe(coupon.ExpiresAt, TimeSpan.FromMilliseconds(1));
        });
    }

    /// <summary>
    /// A waitlist for a fresh event and ticket type, holding one active entry.
    /// </summary>
    private static (Waitlist Waitlist, TicketedEvent TicketedEvent, TicketType TicketType) CreateWaitlistWithOneEntry()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var ticketTypeId = TicketTypeId.New();

        var ticketedEvent = TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            eventId,
            teamId,
            EventName.From("DevConf 2026"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            TimeZoneId.From("UTC"));
        var catalog = TicketCatalog.Create(eventId, teamId);
        catalog.AddTicketType(ticketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity: 1,
            waitlistEnabled: true);

        var waitlist = Waitlist.Create(eventId, ticketTypeId, teamId);
        waitlist.AddEntry(EmailAddress.From("attendee@example.com"), DateTimeOffset.UtcNow);

        return (waitlist, ticketedEvent, catalog.GetTicketType(ticketTypeId)!);
    }
}
