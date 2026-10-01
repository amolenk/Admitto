using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Fixture = Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistedRegistrations.WaitlistedRegistrationsFixture;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistedRegistrations;

/// <summary>
/// The waitlist stops issuing automatic offers when registration closes, whatever frees the seat, and resumes when the
/// registration window is moved to close later. Offers issued before close stay valid until they expire.
/// </summary>
[TestClass]
public sealed class WaitlistRegistrationCloseTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a sold-out ticket type with people waiting, after registration has closed
    // When a registration is cancelled
    // Then nobody is offered the freed seat, and the queue is left as it is
    [TestMethod]
    public async ValueTask CancelRegistration_AfterClose_IssuesNoOffer()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act
        var published = await actions.CancelAsync(fixture.RegisteredIds[1]);

        // Assert
        await AssertQueueLeftAsIsAsync(fixture, actions, expectedAvailable: 1);
        published.OfType<WaitlistCouponIssuedIntegrationEvent>().ShouldBeEmpty();
    }

    // Given an automatic offer issued before registration closed
    // When the attendee registers with it after registration has closed
    // Then the registration succeeds, and the offer's hold becomes a public ticket
    [TestMethod]
    public async ValueTask RegisterWithCoupon_OfferIssuedBeforeClose_RedeemedAfterClose()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        await actions.CancelAsync(fixture.RegisteredIds[1]);
        var couponCode = await actions.GetCouponCodeAsync(Fixture.WaitingEmail(1));
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act
        await actions.RegisterWithCouponAsync(Fixture.WaitingEmail(1), couponCode, fixture.ConferencePassId);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.AsNoTracking()
                .SingleAsync(r => r.Email == Fixture.WaitingEmail(1), testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Registered);

            var ticketType = await actions.GetTicketTypeAsync(dbContext, fixture.ConferencePassId);
            ticketType.WaitlistHeldCapacity.ShouldBe(0);
            ticketType.PublicUsedCapacity.ShouldBe(Fixture.PublicCapacity);
        });
    }

    // Given an automatic offer issued before registration closed
    // When it lapses unclaimed after registration has closed
    // Then its hold is released but nobody else is offered, and the expired-offer email says registration has closed
    [TestMethod]
    public async ValueTask ProcessExpiredWaitlistCoupons_OfferLapsesAfterClose_IssuesNoNewOffer()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        await actions.CancelAsync(fixture.RegisteredIds[1]);
        actions.Clock.SetUtcNow(fixture.AfterOfferLapsed);

        // Act
        var published = await actions.RunExpiredWaitlistCouponsJobAsync();

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var waitlist = await dbContext.Waitlists.AsNoTracking()
                .SingleAsync(w => w.Id == fixture.ConferencePassId, testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Expired);
            waitlist.ActiveEntryCount.ShouldBe(Fixture.WaitingCount - 1);

            var ticketType = await actions.GetTicketTypeAsync(dbContext, fixture.ConferencePassId);
            ticketType.WaitlistHeldCapacity.ShouldBe(0);
            ticketType.AvailableCapacity.ShouldBe(1);
        });
        published.OfType<WaitlistCouponIssuedIntegrationEvent>().ShouldBeEmpty();
        published.OfType<WaitlistCouponExpiredIntegrationEvent>()
            .ShouldHaveSingleItem().RegistrationClosed.ShouldBeTrue();
    }

    // Given a sold-out ticket type with people waiting, after registration has closed
    // When its public capacity is raised
    // Then nobody is offered the new seat
    [TestMethod]
    public async ValueTask UpdateTicketType_RaiseCapacityAfterClose_IssuesNoOffer()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act
        var published = await actions.UpdatePublicCapacityAsync(Fixture.PublicCapacity + 1);

        // Assert
        await AssertQueueLeftAsIsAsync(fixture, actions, expectedAvailable: 1);
        published.OfType<WaitlistCouponIssuedIntegrationEvent>().ShouldBeEmpty();
    }

    // Given a sold-out ticket type with people waiting, after registration has closed
    // When its capacity limit is removed
    // Then nobody is offered a seat, and everyone waiting stays queued
    [TestMethod]
    public async ValueTask UpdateTicketType_RemoveCapacityLimitAfterClose_IssuesNoOffer()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act
        var published = await actions.RemoveCapacityLimitAsync();

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(0);
            var waitlist = await dbContext.Waitlists.AsNoTracking()
                .SingleAsync(w => w.Id == fixture.ConferencePassId, testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(Fixture.WaitingCount);
        });
        published.OfType<WaitlistCouponIssuedIntegrationEvent>().ShouldBeEmpty();
    }

    // Given a capacity limit removed after registration closed, with people still waiting
    // When the registration window is moved to close later, so registration is open again
    // Then everyone waiting is offered a seat, as removing the limit would have done while open
    [TestMethod]
    public async ValueTask ConfigureRegistrationPolicy_ReopenedAfterLimitRemovedWhileClosed_OffersEveryoneWaiting()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        actions.Clock.SetUtcNow(fixture.AfterClose);
        await actions.RemoveCapacityLimitAsync();

        // Act
        var published = await actions.MoveClosesAtAsync(fixture.AfterClose.AddDays(5));

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var waitlist = await dbContext.Waitlists.AsNoTracking()
                .SingleAsync(w => w.Id == fixture.ConferencePassId, testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(0);
        });
        published.OfType<WaitlistCouponIssuedIntegrationEvent>()
            .Select(e => e.RecipientEmail)
            .ShouldBe(Enumerable.Range(1, Fixture.WaitingCount).Select(p => Fixture.WaitingEmail(p).Value), ignoreOrder: true);
    }

    // Given a seat freed after registration closed, with people waiting
    // When the registration window is moved to close later, so registration is open again
    // Then the freed seat is offered to the front of the queue straight away
    [TestMethod]
    public async ValueTask ConfigureRegistrationPolicy_ClosesAtMovedLater_OffersSeatsFreedWhileClosed()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        actions.Clock.SetUtcNow(fixture.AfterClose);
        await actions.CancelAsync(fixture.RegisteredIds[1]);

        // Act
        var published = await actions.MoveClosesAtAsync(fixture.AfterClose.AddDays(5));

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.AsNoTracking().SingleAsync(testContext.CancellationToken)).Email
                .ShouldBe(Fixture.WaitingEmail(1));
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(
                fixture.EventId, fixture.ConferencePassId, testContext.CancellationToken);
        });
        published.OfType<WaitlistCouponIssuedIntegrationEvent>()
            .ShouldHaveSingleItem().RecipientEmail.ShouldBe(Fixture.WaitingEmail(1).Value);
    }

    // Given a sold-out ticket type with people waiting, after registration has closed
    // When the organizer promotes a waiting attendee as a VIP
    // Then the VIP offer is issued, on top of public capacity
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_AfterClose_IssuesVipOffer()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act
        await actions.PromoteAsync(position: 1);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.AsNoTracking().SingleAsync(testContext.CancellationToken);
            coupon.Email.ShouldBe(Fixture.WaitingEmail(1));
            coupon.WaitlistOrigin.ShouldBe(WaitlistCouponOrigin.Manual);
            coupon.BypassRegistrationWindow.ShouldBeTrue();

            var ticketType = await actions.GetTicketTypeAsync(dbContext, fixture.ConferencePassId);
            ticketType.WaitlistHeldCapacity.ShouldBe(0);
        });
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private async ValueTask<(Fixture Fixture, WaitlistedRegistrationsActions Actions)> SetupAsync()
    {
        var fixture = Fixture.SoldOutWithWaitlistedRegistrations();
        await fixture.SetupAsync(Environment);
        return (fixture, new WaitlistedRegistrationsActions(fixture, Environment, testContext.CancellationToken));
    }

    /// <summary>
    /// Asserts nobody was offered a seat: no coupons, every waiting attendee still queued with a <c>Waitlisted</c>
    /// registration, and the seat left publicly available.
    /// </summary>
    private async ValueTask AssertQueueLeftAsIsAsync(
        Fixture fixture,
        WaitlistedRegistrationsActions actions,
        int expectedAvailable)
    {
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            (await dbContext.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(0);

            var waitlist = await dbContext.Waitlists.AsNoTracking()
                .SingleAsync(w => w.Id == fixture.ConferencePassId, testContext.CancellationToken);
            waitlist.ActiveEntryCount.ShouldBe(Fixture.WaitingCount);

            for (var position = 1; position <= Fixture.WaitingCount; position++)
            {
                var registration = await dbContext.Registrations.AsNoTracking()
                    .SingleAsync(r => r.Email == Fixture.WaitingEmail(position), testContext.CancellationToken);
                registration.Status.ShouldBe(RegistrationStatus.Waitlisted);
            }

            var ticketType = await actions.GetTicketTypeAsync(dbContext, fixture.ConferencePassId);
            ticketType.AvailableCapacity.ShouldBe(expectedAvailable);
            ticketType.WaitlistMode.ShouldBeTrue();
        });
    }
}
