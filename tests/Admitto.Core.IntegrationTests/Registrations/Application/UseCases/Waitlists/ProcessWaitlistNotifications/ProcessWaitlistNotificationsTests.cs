using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;

[TestClass]
public sealed class ProcessWaitlistNotificationsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_WithOneEntry_IssuesCouponToTopRankedAttendee()
    {
        // Arrange
        var fixture = ProcessWaitlistNotificationsFixture.WithOneEntryOneSlot();
        await fixture.SetupAsync(Environment, activeEntries: 1);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value, FreedSlots: 1),
            testContext.CancellationToken);

        // Assert — one coupon created, waitlist entry removed
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull();
            coupon.Source.ShouldBe(CouponSource.Waitlist);
            coupon.BypassRegistrationWindow.ShouldBeTrue();
            coupon.AllowedTicketTypeIds.ShouldContain(fixture.TicketTypeId);
            coupon.Email.Value.ShouldBe("attendee1@example.com");

            var waitlist = await dbContext.Waitlists
                .Include(w => w.Entries)
                .FirstOrDefaultAsync(w => w.Id == fixture.TicketTypeId, testContext.CancellationToken);
            waitlist.ShouldNotBeNull();
            waitlist.Entries.ShouldNotContain(e => e.Status == WaitlistEntryStatus.Active);
        });
    }

    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_WithMultipleEntriesAndOneFreedSlot_IssuesSingleCoupon()
    {
        // Arrange
        var fixture = ProcessWaitlistNotificationsFixture.WithTwoEntriesOneSlot();
        await fixture.SetupAsync(Environment, activeEntries: 2);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value, FreedSlots: 1),
            testContext.CancellationToken);

        // Assert — only one coupon, one entry still active (position 2 → renumbered to 1)
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupons = await dbContext.Coupons.ToListAsync(testContext.CancellationToken);
            coupons.Count.ShouldBe(1);

            var waitlist = await dbContext.Waitlists
                .Include(w => w.Entries)
                .FirstOrDefaultAsync(w => w.Id == fixture.TicketTypeId, testContext.CancellationToken);
            waitlist.ShouldNotBeNull();
            waitlist.Entries.Count(e => e.Status == WaitlistEntryStatus.Active).ShouldBe(1);
        });
    }

    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_WhenFewerEntriesThanFreedSlots_IssuesCouponsOnlyForActiveEntries()
    {
        // Arrange — 1 active entry, 2 freed slots
        var fixture = ProcessWaitlistNotificationsFixture.WithOneEntryTwoSlots();
        await fixture.SetupAsync(Environment, activeEntries: 1);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value, FreedSlots: 2),
            testContext.CancellationToken);

        // Assert — only 1 coupon issued (capped by active entry count)
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupons = await dbContext.Coupons.ToListAsync(testContext.CancellationToken);
            coupons.Count.ShouldBe(1);
        });
    }

    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_DuringQuietHours_ExpiryExtendedToAfterQuietHours()
    {
        // Arrange — simulate time at 23:00 UTC (well inside 22:00–08:00 quiet window)
        var quietNightTime = new DateTimeOffset(2026, 6, 15, 23, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(quietNightTime);

        var fixture = ProcessWaitlistNotificationsFixture.WithOneEntryOneSlot();
        await fixture.SetupAsync(Environment, activeEntries: 1);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, fakeTime);

        // Act
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value, FreedSlots: 1),
            testContext.CancellationToken);

        // Assert — expiry must be after quiet hours end (08:00 next day) + 8h = 16:00 next day UTC
        // Since TimeZone is UTC in the fixture, quiet hours span midnight 22:00–08:00.
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull();

            var expectedWindowStart = new DateTimeOffset(2026, 6, 16, 8, 0, 0, TimeSpan.Zero);
            var expectedExpiry = expectedWindowStart.AddHours(8); // 16:00 next day

            coupon.ExpiresAt.ShouldBe(expectedExpiry, tolerance: TimeSpan.FromSeconds(1));
        });
    }
}
