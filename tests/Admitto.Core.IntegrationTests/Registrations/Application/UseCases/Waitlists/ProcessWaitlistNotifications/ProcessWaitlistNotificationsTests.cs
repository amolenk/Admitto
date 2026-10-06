using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;

[TestClass]
public sealed class ProcessWaitlistNotificationsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a waitlist with one active entry and one free seat
    // When waitlist notifications are processed
    // Then a coupon is issued to the top-ranked attendee and their entry is removed
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
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value),
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

    // Given a waitlist with two active entries and only one free seat
    // When waitlist notifications are processed
    // Then only one coupon is issued and the remaining entry is renumbered to the top position
    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_WithMultipleEntriesAndOneFreeSeat_IssuesSingleCoupon()
    {
        // Arrange
        var fixture = ProcessWaitlistNotificationsFixture.WithTwoEntriesOneSlot();
        await fixture.SetupAsync(Environment, activeEntries: 2);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value),
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

            var catalog = await dbContext.TicketCatalogs
                .FirstAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);
            catalog.GetTicketType(fixture.TicketTypeId)!.WaitlistHeldCapacity.ShouldBe(1);
        });
    }

    // Given a ticket type with one free seat and a VIP offer outstanding that was made while it was sold out
    // When waitlist notifications are processed
    // Then the VIP offer takes no public seat, so the free seat goes to the front of the queue
    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_FreeSeatWithVipOfferOutstanding_OffersFrontOfQueue()
    {
        // Arrange
        var fixture = ProcessWaitlistNotificationsFixture.WithTwoEntriesOneSlotAndVipOffer();
        await fixture.SetupAsync(Environment, activeEntries: 2);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value),
            testContext.CancellationToken);

        // Assert — the VIP's coupon and one new offer to the front of the queue
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.Select(c => c.Email.Value).ToListAsync(testContext.CancellationToken))
                .ShouldBe(["attendee3@example.com", "attendee1@example.com"], ignoreOrder: true);

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(1);
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(
                fixture.EventId, fixture.TicketTypeId, testContext.CancellationToken);
        });
    }

    // Given a waitlist with one active entry but two free seats
    // When waitlist notifications are processed
    // Then coupons are issued only for the active entries, capped at one
    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_WhenFewerEntriesThanFreeSeats_IssuesCouponsOnlyForActiveEntries()
    {
        // Arrange — 1 active entry, 2 free seats
        var fixture = ProcessWaitlistNotificationsFixture.WithOneEntryTwoSlots();
        await fixture.SetupAsync(Environment, activeEntries: 1);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value),
            testContext.CancellationToken);

        // Assert — only 1 coupon issued (capped by active entry count)
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupons = await dbContext.Coupons.ToListAsync(testContext.CancellationToken);
            coupons.Count.ShouldBe(1);
        });
    }

    // Given a sold-out ticket type with an attendee still waiting and no outstanding coupons
    // When waitlist notifications are processed without any free seat
    // Then no coupon is issued and waitlist mode stays on
    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_NoFreeSeatWithActiveEntries_KeepsWaitlistMode()
    {
        // Arrange
        var fixture = ProcessWaitlistNotificationsFixture.WithOneEntryNoSlots();
        await fixture.SetupAsync(Environment, activeEntries: 1);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.AnyAsync(testContext.CancellationToken)).ShouldBeFalse();

            var catalog = await dbContext.TicketCatalogs
                .FirstAsync(tc => tc.Id == fixture.EventId, testContext.CancellationToken);
            catalog.GetTicketType(fixture.TicketTypeId)!.WaitlistMode.ShouldBeTrue();
        });
    }

    // Given the current time falls inside the event's quiet hours window
    // When waitlist notifications are processed
    // Then the issued coupon's expiry is extended to after the quiet hours end
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
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value),
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

    // Given one free seat at 18:00 and quiet hours from 20:00 to 08:00
    // When an eight-hour waitlist offer is issued
    // Then the coupon and tracked offer expire at 14:00 the next day
    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_BeforeQuietHours_PreservesRemainingClaimTime()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero));
        var fixture = ProcessWaitlistNotificationsFixture.WithOneEntryAndQuietHours20To08();
        await fixture.SetupAsync(Environment);
        var sut = new ProcessWaitlistNotificationsHandler(Environment.RegistrationsDatabase.Context, fakeTime);

        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var expectedExpiry = new DateTimeOffset(2026, 6, 16, 14, 0, 0, TimeSpan.Zero);
            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.ExpiresAt.ShouldBe(expectedExpiry);

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().ExpiresAt.ShouldBe(expectedExpiry);
        });
    }

    // Given a waitlist with one active entry and one free seat
    // When waitlist notifications are processed
    // Then the promoted attendee's waitlist offer email is prepared with the correct coupon and expiry
    [TestMethod]
    public async ValueTask ProcessWaitlistNotifications_WithOneEntry_PreparesWaitlistOfferEmailForPromotedAttendee()
    {
        // Arrange
        var fixture = ProcessWaitlistNotificationsFixture.WithOneEntryOneSlot();
        await fixture.SetupAsync(Environment, activeEntries: 1);

        var sut = new ProcessWaitlistNotificationsHandler(
            Environment.RegistrationsDatabase.Context, TimeProvider.System);

        // Act — run the automatic promotion
        await sut.HandleAsync(
            new ProcessWaitlistNotificationsCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value),
            testContext.CancellationToken);

        // Assert — the promotion raised a WaitlistCouponIssuedDomainEvent with the coupon details
        var waitlist = await Environment.RegistrationsDatabase.Context.Waitlists
            .FirstAsync(w => w.Id == fixture.TicketTypeId, testContext.CancellationToken);
        var domainEvent = waitlist.GetDomainEvents()
            .OfType<WaitlistCouponIssuedDomainEvent>()
            .ShouldHaveSingleItem();

        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
        var coupon = await Environment.RegistrationsDatabase.Context.Coupons
            .SingleAsync(testContext.CancellationToken);

        domainEvent.RecipientEmail.Value.ShouldBe("attendee1@example.com");
        domainEvent.CouponCode.ShouldBe(coupon.Code);
        domainEvent.TicketTypeName.ShouldBe("Conference Pass");
        domainEvent.ExpiresAt.ShouldBe(coupon.ExpiresAt);

        // Act — publish the domain event as the real integration event publisher would
        var outbox = Substitute.For<IOutbox>();
        IIntegrationEvent? capturedIntegrationEvent = null;
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>()))
            .Do(ci => capturedIntegrationEvent = ci.Arg<IIntegrationEvent>());
        var publisher = new RegistrationsIntegrationEventPublisher(outbox);

        await publisher.HandleAsync(domainEvent, testContext.CancellationToken);

        var integrationEvent = capturedIntegrationEvent.ShouldBeOfType<WaitlistCouponIssuedIntegrationEvent>();

        // Act — hand the integration event to the existing email adapter
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.WaitlistNotification);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var emailHandler = new WaitlistCouponIssuedIntegrationEventHandler(composer, deliveryHandler, NullLogger<WaitlistCouponIssuedIntegrationEventHandler>.Instance);

        await emailHandler.HandleAsync(integrationEvent, testContext.CancellationToken);

        // Assert — an email delivery was prepared with the correct recipient, ticket type, coupon code, and expiry
        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("attendee1@example.com");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistNotification);
        integrationEvent.TicketTypeName.ShouldBe("Conference Pass");
        integrationEvent.CouponCode.ShouldBe(coupon.Code.Value.ToString());
        integrationEvent.ExpiresAt.ShouldBe(coupon.ExpiresAt);
    }
}
