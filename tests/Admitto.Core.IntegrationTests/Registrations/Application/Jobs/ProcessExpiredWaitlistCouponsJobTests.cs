using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.Jobs;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.Jobs;

/// <summary>
/// The job runs on a <see cref="DispatchingRegistrationsContext"/>, so releasing an expired offer's hold cascades
/// through the real domain event handlers in the same save, as in production.
/// </summary>
[TestClass]
public sealed class ProcessExpiredWaitlistCouponsJobTests(TestContext testContext) : AspireIntegrationTestBase
{
    private DispatchingRegistrationsContext _dispatch = null!;

    [TestInitialize]
    public override async ValueTask TestInitialize()
    {
        await base.TestInitialize();
        _dispatch = DispatchingRegistrationsContext.Create(Environment);
    }

    [TestCleanup]
    public async ValueTask TestCleanup() => await _dispatch.DisposeAsync();

    // Given a waitlist coupon expired past its grace period with another entry still waiting
    // When the process-expired-waitlist-coupons job runs
    // Then the lapsed coupon is expired and a fresh coupon is issued to the next waitlist entry
    [TestMethod]
    public async ValueTask Execute_WhenCouponIsExpiredAndWaitlistHasNextEntry_ExpiresAndNotifiesNext()
    {
        // Arrange — expired coupon (10 min past grace), one more person waiting
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithTwoEntriesOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 1, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        var job = CreateJob();
        var quartzContext = QuartzContext();

        // Act
        await job.Execute(quartzContext);

        // Assert — original coupon expired, a fresh coupon issued to the next person
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldBe(new Dictionary<string, WaitlistCouponStatus>
            {
                ["attendee1@example.com"] = WaitlistCouponStatus.Expired,
                ["attendee2@example.com"] = WaitlistCouponStatus.Issued,
            }, ignoreOrder: true);
            await ctx.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given the last pending waitlist coupon expired past its grace period with no remaining waitlist entries
    // When the process-expired-waitlist-coupons job runs
    // Then the coupon is expired and the ticket type's waitlist mode is cleared
    [TestMethod]
    public async ValueTask Execute_WhenLastCouponExpiresAndWaitlistIsEmpty_LiftsWaitlistMode()
    {
        // Arrange — one entry, one coupon, empty waitlist after expiry
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithOneEntryOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 0, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        var job = CreateJob();
        var quartzContext = QuartzContext();

        // Act
        await job.Execute(quartzContext);

        // Assert — coupon expired, and WaitlistMode cleared on the catalog (no remaining entries or coupons)
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldHaveSingleItem().Value.ShouldBe(WaitlistCouponStatus.Expired);

            var catalog = await ctx.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();

            var ticketType = catalog.GetTicketType(fixture.TicketTypeId);
            ticketType.ShouldNotBeNull();
            ticketType.WaitlistMode.ShouldBeFalse(
                "WaitlistMode should be cleared when the last coupon expires and no entries remain");
        });
    }

    // Given a waitlist coupon that expired but is still within its grace period
    // When the process-expired-waitlist-coupons job runs
    // Then the coupon is left issued
    [TestMethod]
    public async ValueTask Execute_WhenCouponIsWithinGracePeriod_DoesNotExpire()
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
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldHaveSingleItem().Value.ShouldBe(
                WaitlistCouponStatus.Issued, "coupon within the grace period must not be expired");
        });
    }

    // Given two waitlist coupons that expired unclaimed past their grace period with another entry still waiting
    // When the process-expired-waitlist-coupons job runs
    // Then exactly one expired-offer email is prepared for each attendee whose coupon lapsed
    [TestMethod]
    public async ValueTask Execute_WhenTwoCouponsExpire_PreparesOneExpiredOfferEmailPerExpiredCoupon()
    {
        // Arrange — two expired coupons (attendee1, attendee2), attendee3 still waiting
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithThreeEntriesTwoPendingCoupons();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 1, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        var job = CreateJob();

        // Act — run the expiry job
        await job.Execute(QuartzContext());

        // Assert — one expired-offer integration event per lapsed coupon, addressed to its recipient
        var expiredEvents = ExpiredOfferEvents();
        expiredEvents.Select(e => e.RecipientEmail)
            .ShouldBe(["attendee1@example.com", "attendee2@example.com"], ignoreOrder: true);
        expiredEvents.ShouldAllBe(e => e.TicketTypeName == "Conference Pass");

        // Act — hand the integration events to the expired-offer email adapter
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.WaitlistOfferExpired);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var emailHandler = new WaitlistCouponExpiredIntegrationEventHandler(composer, deliveryHandler);
        foreach (var integrationEvent in expiredEvents)
            await emailHandler.HandleAsync(integrationEvent, testContext.CancellationToken);

        // Assert — exactly one expired-offer email per lapsed coupon, none for the still-waiting attendee
        var deliveries = deliveryHandler.ReceivedCalls()
            .Select(call => (PrepareEmailDeliveryCommand)call.GetArguments()[0]!)
            .ToList();
        deliveries.Select(d => d.RecipientAddress)
            .ShouldBe(["attendee1@example.com", "attendee2@example.com"], ignoreOrder: true);
        deliveries.ShouldAllBe(d => d.EmailType == BuiltInEmailTemplateNames.WaitlistOfferExpired);
        deliveries.Select(d => d.IdempotencyKey).Distinct().Count().ShouldBe(2);
    }

    // Given the last pending waitlist coupon expired past its grace period with no remaining waitlist entries
    // When the process-expired-waitlist-coupons job runs
    // Then exactly one expired-offer event is still raised for the attendee whose coupon lapsed
    [TestMethod]
    public async ValueTask Execute_WhenLastCouponExpiresAndWaitlistIsEmpty_RaisesOneExpiredOfferEvent()
    {
        // Arrange — one entry, one coupon, nobody left waiting after it lapses
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithOneEntryOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 0, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        var job = CreateJob();

        // Act
        await job.Execute(QuartzContext());

        // Assert
        ExpiredOfferEvents().ShouldHaveSingleItem().RecipientEmail.ShouldBe("attendee1@example.com");
    }

    // Given a waitlist coupon that expired but is still within its grace period
    // When the process-expired-waitlist-coupons job runs
    // Then no expired-offer event is raised
    [TestMethod]
    public async ValueTask Execute_WhenCouponIsWithinGracePeriod_RaisesNoExpiredOfferEvent()
    {
        // Arrange — coupon expired 1 min ago, still inside the 2-minute grace window
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithOneEntryOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 0, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(1), testContext.CancellationToken);

        var job = CreateJob();

        // Act
        await job.Execute(QuartzContext());

        // Assert — the waitlist was never touched, so no expired-offer event exists
        ExpiredOfferEvents().ShouldBeEmpty();
    }

    // Given a VIP coupon made while sold out, expired, with another entry still waiting
    // When the process-expired-waitlist-coupons job runs
    // Then the coupon is expired and its recipient told, but no new offer goes out
    [TestMethod]
    public async ValueTask Execute_WhenVipCouponExpiresWhileSoldOut_ExpiresWithoutNotifyingNext()
    {
        // Arrange — attendee2 promoted as VIP past attendee1, who is still waiting
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithOnePendingVipCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 1, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        // Act
        await CreateJob().Execute(QuartzContext());

        // Assert — the expired-offer event is raised for the VIP recipient
        ExpiredOfferEvents().ShouldHaveSingleItem().RecipientEmail.ShouldBe("attendee2@example.com");

        // Assert — the VIP coupon is expired, no coupon went to the waiting attendee, WaitlistMode stays on
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldHaveSingleItem().ShouldBe(
                new KeyValuePair<string, WaitlistCouponStatus>("attendee2@example.com", WaitlistCouponStatus.Expired));

            var catalog = await ctx.TicketCatalogs
                .FirstAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);
            catalog.GetTicketType(fixture.TicketTypeId)!.WaitlistMode.ShouldBeTrue(
                "WaitlistMode stays on while an attendee is still waiting");
            await ctx.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given an automatic and a VIP coupon that both expired, with two more entries still waiting
    // When the process-expired-waitlist-coupons job runs
    // Then both coupons are expired but only one new offer goes out, for the one seat that is available
    [TestMethod]
    public async ValueTask Execute_WhenAutomaticAndVipCouponsExpire_OffersOnlyTheAvailableSeat()
    {
        // Arrange — attendee1 has an automatic coupon, attendee4 a VIP coupon; attendee2 and attendee3 wait
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithOnePendingCouponAndOnePendingVipCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 2, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        // Act
        await CreateJob().Execute(QuartzContext());

        // Assert — one fresh offer, to the front of the queue
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldBe(new Dictionary<string, WaitlistCouponStatus>
            {
                ["attendee1@example.com"] = WaitlistCouponStatus.Expired,
                ["attendee4@example.com"] = WaitlistCouponStatus.Expired,
                ["attendee2@example.com"] = WaitlistCouponStatus.Issued,
            }, ignoreOrder: true);
            await ctx.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given an automatic offer to the front of the queue, then a VIP offer to the next attendee made while sold out
    // When only the automatic offer lapses and the process-expired-waitlist-coupons job runs
    // Then the freed seat covers the VIP offer, so nobody else in the queue is offered
    [TestMethod]
    public async ValueTask Execute_WhenAutomaticCouponExpiresWhileVipOfferOutstanding_DoesNotNotifyNext()
    {
        // Arrange — attendee1 automatic, attendee2 VIP (position 2), attendee3 waiting
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithPendingCouponAndVipCouponAtPositionTwo();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 1, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(
            Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken, recipient: "attendee1@example.com");

        // Act
        await CreateJob().Execute(QuartzContext());

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldBe(new Dictionary<string, WaitlistCouponStatus>
            {
                ["attendee1@example.com"] = WaitlistCouponStatus.Expired,
                ["attendee2@example.com"] = WaitlistCouponStatus.Issued,
            }, ignoreOrder: true);

            var waitlist = await ctx.Waitlists.SingleAsync(w => w.Id == fixture.TicketTypeId, testContext.CancellationToken);
            waitlist.GetActivePosition(EmailAddress.From("attendee3@example.com")).ShouldBe(1);

            var ticketType = (await ctx.TicketCatalogs.SingleAsync(testContext.CancellationToken))
                .FindTicketType(fixture.TicketTypeId);
            ticketType.AvailableCapacity.ShouldBe(0);
            await ctx.ShouldHoldOneSeatPerIssuedCouponAsync(fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given a waitlist coupon expired past its grace period
    // When an organizer registers an attendee on the same ticket type while the job is running
    // Then the job cannot commit on the catalog it read, and fails with a concurrency conflict
    [TestMethod]
    public async ValueTask Execute_ConcurrentOrganiserRegistration_JobFailsWithConcurrencyConflict()
    {
        // Arrange — the job's context has already read the catalog when the organizer's registration commits
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithTwoEntriesOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 1, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        await _dispatch.Context.TicketCatalogs.SingleAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);

        await using (var organiser = DispatchingRegistrationsContext.Create(Environment))
        {
            await new AdminRegisterAttendeeHandler(organiser.Context, TimeProvider.System).HandleAsync(
                new AdminRegisterAttendeeCommand(
                    fixture.EventId.Value,
                    fixture.TeamId.Value,
                    "organiser-guest@example.com",
                    "Grace",
                    "Hopper",
                    [fixture.TicketTypeId.Value],
                    AdditionalDetails: null),
                testContext.CancellationToken);
            await organiser.SaveChangesAsync(testContext.CancellationToken);
        }

        // Act
        var exception = await Should.ThrowAsync<JobExecutionException>(
            async () => await CreateJob().Execute(QuartzContext()));

        // Assert
        exception.InnerException.ShouldBeOfType<DbUpdateConcurrencyException>();
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldHaveSingleItem().Value.ShouldBe(WaitlistCouponStatus.Issued);
        });
    }

    // Given an expired VIP coupon with nobody left waiting and no other outstanding coupons
    // When the process-expired-waitlist-coupons job runs
    // Then the coupon is expired and the ticket type's waitlist mode is cleared
    [TestMethod]
    public async ValueTask Execute_WhenVipCouponExpiresAndWaitlistIsEmpty_LiftsWaitlistMode()
    {
        // Arrange — the only entry was promoted as VIP; nobody else is waiting
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithOnePendingVipCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 0, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        // Act
        await CreateJob().Execute(QuartzContext());

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldHaveSingleItem().Value.ShouldBe(WaitlistCouponStatus.Expired);

            var catalog = await ctx.TicketCatalogs
                .FirstAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);
            catalog.GetTicketType(fixture.TicketTypeId)!.WaitlistMode.ShouldBeFalse(
                "WaitlistMode should be cleared when the VIP coupon expires and no entries or coupons remain");
        });
    }

    // Given an unclaimed waitlist offer on a ticket type whose waitlist the organizer has since disabled
    // When the offer lapses and the process-expired-waitlist-coupons job runs
    // Then the coupon is expired and its recipient still gets the expired-offer email, but nobody else is offered
    [TestMethod]
    public async ValueTask Execute_WhenCouponExpiresAfterWaitlistDisabled_RaisesExpiredOfferEventWithoutCascading()
    {
        // Arrange — one offer outstanding and one person waiting when the waitlist is disabled
        var fixture = ProcessExpiredWaitlistCouponsJobFixture.WithTwoEntriesOnePendingCoupon();
        await fixture.SetupAsync(Environment, activeEntriesAfterCoupon: 1, testContext.CancellationToken);
        await fixture.DisableWaitlistAsync(Environment, testContext.CancellationToken);
        await fixture.BackdateCouponExpiryAsync(Environment, TimeSpan.FromMinutes(10), testContext.CancellationToken);

        // Act
        await CreateJob().Execute(QuartzContext());

        // Assert
        ExpiredOfferEvents().ShouldHaveSingleItem().RecipientEmail.ShouldBe("attendee1@example.com");
        _dispatch.PublishedIntegrationEvents.OfType<WaitlistCouponIssuedIntegrationEvent>().ShouldBeEmpty();

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var statuses = await WaitlistCouponStatusesByEmailAsync(ctx, fixture.TicketTypeId);
            statuses.ShouldHaveSingleItem().Value.ShouldBe(WaitlistCouponStatus.Expired);
            (await ctx.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(1);
        });
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private ProcessExpiredWaitlistCouponsJob CreateJob() =>
        new(_dispatch.Context,
            _dispatch.UnitOfWork,
            TimeProvider.System,
            NullLogger<ProcessExpiredWaitlistCouponsJob>.Instance);

    private List<WaitlistCouponExpiredIntegrationEvent> ExpiredOfferEvents() =>
        _dispatch.PublishedIntegrationEvents.OfType<WaitlistCouponExpiredIntegrationEvent>().ToList();

    /// <summary>
    /// The status of each coupon the waitlist tracks, keyed by the recipient's email.
    /// </summary>
    private async ValueTask<Dictionary<string, WaitlistCouponStatus>> WaitlistCouponStatusesByEmailAsync(
        RegistrationsDbContext ctx,
        TicketTypeId ticketTypeId)
    {
        var waitlist = await ctx.Waitlists.SingleAsync(w => w.Id == ticketTypeId, testContext.CancellationToken);
        var emails = await ctx.Coupons.ToDictionaryAsync(c => c.Id, c => c.Email.Value, testContext.CancellationToken);
        return waitlist.Coupons.ToDictionary(c => emails[c.Id], c => c.Status);
    }

    private IJobExecutionContext QuartzContext()
    {
        var ctx = Substitute.For<IJobExecutionContext>();
        ctx.CancellationToken.Returns(testContext.CancellationToken);
        return ctx;
    }
}
