using Amolenk.Admitto.Core.Registrations.Application.Jobs;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.Jobs;

[TestClass]
public sealed class ProcessExpiredWaitlistCouponsJobTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask Execute_WhenCouponIsExpiredAndWaitlistHasNextEntry_RevokesAndNotifiesNext()
    {
        // Arrange — expired coupon (10 min past grace), one more person waiting
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithTwoEntriesOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 1, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        var job = CreateJob();
        var quartzContext = QuartzContext();

        // Act
        await job.Execute(quartzContext);

        // Assert — original coupon revoked, a fresh coupon issued to the next person
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var coupons = await ctx.Coupons.ToListAsync(testContext.CancellationToken);
            coupons.Count.ShouldBe(2);
            coupons.ShouldContain(c => c.RevokedAt != null, "original coupon should be revoked");
            coupons.ShouldContain(c => c.RevokedAt == null && c.Email.Value == "attendee2@example.com",
                "second attendee should have received a fresh coupon");
        });
    }

    [TestMethod]
    public async ValueTask Execute_WhenLastCouponExpiresAndWaitlistIsEmpty_LiftsWaitlistMode()
    {
        // Arrange — one entry, one coupon, empty waitlist after revocation
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithOneEntryOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 0, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        var job = CreateJob();
        var quartzContext = QuartzContext();

        // Act
        await job.Execute(quartzContext);

        // Assert — coupon revoked, and WaitlistMode cleared on the catalog (no remaining entries or coupons)
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var coupon = await ctx.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull();
            coupon.RevokedAt.ShouldNotBeNull("the expired coupon should have been revoked");

            var catalog = await ctx.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();

            var ticketType = catalog.GetTicketType(fixture.TicketTypeId);
            ticketType.ShouldNotBeNull();
            ticketType.WaitlistMode.ShouldBeFalse(
                "WaitlistMode should be cleared when the last coupon expires and no entries remain");
        });
    }

    [TestMethod]
    public async ValueTask Execute_WhenCouponIsWithinGracePeriod_DoesNotRevoke()
    {
        // Arrange — coupon expired 1 min ago, still inside the 2-minute grace window
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithOneEntryOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 0, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(1), testContext.CancellationToken);

        var job = CreateJob();
        var quartzContext = QuartzContext();

        // Act
        await job.Execute(quartzContext);

        // Assert — coupon untouched (grace period protection)
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var coupon = await ctx.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull();
            coupon.RevokedAt.ShouldBeNull("coupon within the grace period must not be revoked");
        });
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private ProcessExpiredWaitlistCouponsJob CreateJob() =>
        new(Environment.RegistrationsDatabase.Context,
            new ProcessWaitlistNotificationsHandler(
                Environment.RegistrationsDatabase.Context, TimeProvider.System),
            new DbContextUnitOfWork(Environment.RegistrationsDatabase.Context),
            TimeProvider.System,
            NullLogger<ProcessExpiredWaitlistCouponsJob>.Instance);

    private IJobExecutionContext QuartzContext()
    {
        var ctx = Substitute.For<IJobExecutionContext>();
        ctx.CancellationToken.Returns(testContext.CancellationToken);
        return ctx;
    }
}
