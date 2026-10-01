using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Fixture = Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistedRegistrations.WaitlistedRegistrationsFixture;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.WaitlistedRegistrations;

/// <summary>
/// A waitlisted attendee claims their waitlist offer, or is registered by an admin, starting from a real
/// <c>Waitlisted</c> registration. Each test asserts the pool accounting of ADR-019 with no double count.
/// </summary>
[TestClass]
public sealed class ClaimWaitlistOfferTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a waitlisted attendee who got an automatic offer when a seat was freed
    // When they register with the offer's coupon
    // Then their registration becomes Registered, keeps its other waitlist entry, and the offer's hold becomes a public ticket
    [TestMethod]
    public async ValueTask RegisterWithCoupon_WaitlistedAttendeeAutomaticOffer_BecomesRegisteredWithPublicTicket()
    {
        // Arrange — a cancellation offers the freed seat to waiting1
        var (fixture, actions) = await SetupAsync();
        await actions.CancelAsync(fixture.RegisteredIds[1]);
        var couponCode = await actions.GetCouponCodeAsync(Fixture.WaitingEmail(1));
        var before = await actions.GetConferencePassAsync();

        // Act
        await actions.RegisterWithCouponAsync(Fixture.WaitingEmail(1), couponCode, fixture.ConferencePassId);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await GetRegistrationAsync(dbContext, Fixture.WaitingEmail(1));
            registration.Id.ShouldBe(fixture.WaitingRegistrationId(1));
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Tickets.ShouldHaveSingleItem().Mode.ShouldBe(ClaimMode.Public);
            (await IsQueuedAsync(dbContext, fixture.WorkshopId, Fixture.WaitingEmail(1))).ShouldBeTrue();

            var after = await actions.GetTicketTypeAsync(dbContext, fixture.ConferencePassId);
            after.WaitlistHeldCapacity.ShouldBe(before.WaitlistHeldCapacity - 1);
            after.PublicUsedCapacity.ShouldBe(before.PublicUsedCapacity + 1);
            after.AdminUsedCount.ShouldBe(before.AdminUsedCount);
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(
                fixture.EventId, fixture.ConferencePassId, testContext.CancellationToken);
        });
    }

    // Given a waitlisted attendee promoted as a VIP
    // When they register with the VIP coupon
    // Then their registration becomes Registered with an admin ticket, and the public counters are unchanged
    [TestMethod]
    public async ValueTask RegisterWithCoupon_WaitlistedAttendeeVipOffer_BecomesRegisteredWithAdminTicket()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        var couponCode = await actions.PromoteAsync(position: 2);
        var before = await actions.GetConferencePassAsync();

        // Act
        await actions.RegisterWithCouponAsync(Fixture.WaitingEmail(2), couponCode, fixture.ConferencePassId);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await GetRegistrationAsync(dbContext, Fixture.WaitingEmail(2));
            registration.Id.ShouldBe(fixture.WaitingRegistrationId(2));
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Tickets.ShouldHaveSingleItem().Mode.ShouldBe(ClaimMode.Admin);

            var after = await actions.GetTicketTypeAsync(dbContext, fixture.ConferencePassId);
            after.AdminUsedCount.ShouldBe(before.AdminUsedCount + 1);
            after.PublicUsedCapacity.ShouldBe(before.PublicUsedCapacity);
            after.WaitlistHeldCapacity.ShouldBe(before.WaitlistHeldCapacity);
        });
    }

    // Given a waitlisted attendee holding an automatic offer, with more people waiting
    // When an admin registers them for that ticket type
    // Then they get an admin ticket, the offer expires without an expired-offer email, and its seat goes to the next person
    [TestMethod]
    public async ValueTask AdminRegister_WaitlistedAttendeeHoldingAutomaticOffer_ExpiresOfferAndOffersNextInQueue()
    {
        // Arrange — waiting1 holds the automatic offer for the freed seat
        var (fixture, actions) = await SetupAsync();
        await actions.CancelAsync(fixture.RegisteredIds[1]);
        var offeredCouponCode = await actions.GetCouponCodeAsync(Fixture.WaitingEmail(1));
        var before = await actions.GetConferencePassAsync();

        // Act
        var published = await actions.AdminRegisterAsync(Fixture.WaitingEmail(1), fixture.ConferencePassId);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await GetRegistrationAsync(dbContext, Fixture.WaitingEmail(1));
            registration.Id.ShouldBe(fixture.WaitingRegistrationId(1));
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Tickets.ShouldHaveSingleItem().Mode.ShouldBe(ClaimMode.Admin);
            (await IsQueuedAsync(dbContext, fixture.WorkshopId, Fixture.WaitingEmail(1))).ShouldBeTrue();

            var offered = await dbContext.Coupons.SingleAsync(
                c => c.Code == CouponCode.From(offeredCouponCode), testContext.CancellationToken);
            offered.GetStatus(actions.Clock.GetUtcNow()).ShouldBe(CouponStatus.Expired);
            offered.RedeemedAt.ShouldBeNull();

            var next = await dbContext.Coupons.SingleAsync(
                c => c.Email == Fixture.WaitingEmail(2), testContext.CancellationToken);
            next.WaitlistOrigin.ShouldBe(WaitlistCouponOrigin.Automatic);

            var after = await actions.GetTicketTypeAsync(dbContext, fixture.ConferencePassId);
            after.AdminUsedCount.ShouldBe(before.AdminUsedCount + 1);
            after.PublicUsedCapacity.ShouldBe(before.PublicUsedCapacity);
            after.WaitlistHeldCapacity.ShouldBe(1, "the released hold is taken by the next person's offer");
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(
                fixture.EventId, fixture.ConferencePassId, testContext.CancellationToken);
            await dbContext.ShouldCountEveryActiveEntryAsync(
                fixture.EventId, fixture.ConferencePassId, testContext.CancellationToken);
        });
        published.OfType<WaitlistCouponExpiredIntegrationEvent>().ShouldBeEmpty();
        published.OfType<WaitlistCouponIssuedIntegrationEvent>()
            .ShouldHaveSingleItem().RecipientEmail.ShouldBe(Fixture.WaitingEmail(2).Value);
    }

    // Given a waitlisted attendee still in the queue
    // When an admin registers them for that ticket type
    // Then their waitlist entry is removed and the people behind them move up
    [TestMethod]
    public async ValueTask AdminRegister_WaitlistedAttendeeInQueue_RemovesMatchingEntry()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();

        // Act
        await actions.AdminRegisterAsync(Fixture.WaitingEmail(1), fixture.ConferencePassId);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await GetRegistrationAsync(dbContext, Fixture.WaitingEmail(1));
            registration.Status.ShouldBe(RegistrationStatus.Registered);

            var waitlist = await dbContext.Waitlists.SingleAsync(
                w => w.Id == fixture.ConferencePassId, testContext.CancellationToken);
            waitlist.HasActiveEntry(Fixture.WaitingEmail(1)).ShouldBeFalse();
            waitlist.GetActivePosition(Fixture.WaitingEmail(2)).ShouldBe(1);
            (await IsQueuedAsync(dbContext, fixture.WorkshopId, Fixture.WaitingEmail(1))).ShouldBeTrue();
            await dbContext.ShouldCountEveryActiveEntryAsync(
                fixture.EventId, fixture.ConferencePassId, testContext.CancellationToken);
        });
    }

    // Given a waitlisted attendee who got an automatic offer before registration closed
    // When they claim it through update-registration after registration has closed
    // Then the offer bypasses the closed window, and its hold becomes a public ticket
    [TestMethod]
    public async ValueTask UpdateRegistration_WithCouponAfterClose_ClaimsOffer()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        await actions.CancelAsync(fixture.RegisteredIds[1]);
        var couponCode = await actions.GetCouponCodeAsync(Fixture.WaitingEmail(1));
        var before = await actions.GetConferencePassAsync();
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act — confirm the Conference Pass, stay queued for the Workshop
        await actions.UpdateAsync(
            fixture.WaitingRegistrationId(1), [fixture.ConferencePassId], [fixture.WorkshopId], couponCode);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await GetRegistrationAsync(dbContext, Fixture.WaitingEmail(1));
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Tickets.ShouldHaveSingleItem().Mode.ShouldBe(ClaimMode.Public);
            (await IsQueuedAsync(dbContext, fixture.WorkshopId, Fixture.WaitingEmail(1))).ShouldBeTrue();

            var after = await actions.GetTicketTypeAsync(dbContext, fixture.ConferencePassId);
            after.WaitlistHeldCapacity.ShouldBe(before.WaitlistHeldCapacity - 1);
            after.PublicUsedCapacity.ShouldBe(before.PublicUsedCapacity + 1);
            await dbContext.ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(
                fixture.EventId, fixture.ConferencePassId, testContext.CancellationToken);
        });
    }

    // Given a waitlisted attendee with an offer, after registration has closed
    // When they update their registration with the coupon but also join another waitlist
    // Then the coupon only bypasses the window for what it grants, so the update is rejected
    [TestMethod]
    public async ValueTask UpdateRegistration_WithCouponAfterCloseAndWaitlistJoin_ThrowsRegistrationClosed()
    {
        // Arrange — waiting2 holds a VIP offer and isn't queued for the Workshop
        var (fixture, actions) = await SetupAsync();
        var couponCode = await actions.PromoteAsync(position: 2);
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act
        var result = await ErrorResult.CaptureAsync(async () =>
        {
            await actions.UpdateAsync(
                fixture.WaitingRegistrationId(2), [fixture.ConferencePassId], [fixture.WorkshopId], couponCode);
        });

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.RegistrationClosed);
    }

    // Given a waitlisted attendee with an automatic offer, after registration has closed
    // When they claim it through update-registration but also leave their other waitlist
    // Then the coupon only bypasses the window for what it grants, so the update is rejected
    [TestMethod]
    public async ValueTask UpdateRegistration_WithCouponAfterCloseAndWaitlistLeave_ThrowsRegistrationClosed()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        await actions.CancelAsync(fixture.RegisteredIds[1]);
        var couponCode = await actions.GetCouponCodeAsync(Fixture.WaitingEmail(1));
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act
        var result = await ErrorResult.CaptureAsync(async () =>
        {
            await actions.UpdateAsync(fixture.WaitingRegistrationId(1), [fixture.ConferencePassId], [], couponCode);
        });

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.RegistrationClosed);
    }

    // Given a waitlisted attendee, after registration has closed
    // When they update their registration without a coupon
    // Then the registration window is enforced
    [TestMethod]
    public async ValueTask UpdateRegistration_WithoutCouponAfterClose_ThrowsRegistrationClosed()
    {
        // Arrange
        var (fixture, actions) = await SetupAsync();
        actions.Clock.SetUtcNow(fixture.AfterClose);

        // Act
        var result = await ErrorResult.CaptureAsync(async () =>
        {
            await actions.UpdateAsync(fixture.WaitingRegistrationId(1), [], [fixture.ConferencePassId]);
        });

        // Assert
        result.Error.ShouldMatch(TicketedEvent.Errors.RegistrationClosed);
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private async ValueTask<(Fixture Fixture, WaitlistedRegistrationsActions Actions)> SetupAsync()
    {
        var fixture = Fixture.SoldOutWithWaitlistedRegistrations();
        await fixture.SetupAsync(Environment);
        return (fixture, new WaitlistedRegistrationsActions(fixture, Environment, testContext.CancellationToken));
    }

    private ValueTask<Registration> GetRegistrationAsync(RegistrationsDbContext dbContext, EmailAddress email) =>
        new(dbContext.Registrations.AsNoTracking().SingleAsync(r => r.Email == email, testContext.CancellationToken));

    private async ValueTask<bool> IsQueuedAsync(RegistrationsDbContext dbContext, TicketTypeId ticketTypeId, EmailAddress email)
    {
        var waitlist = await dbContext.Waitlists.AsNoTracking()
            .SingleAsync(w => w.Id == ticketTypeId, testContext.CancellationToken);
        return waitlist.HasActiveEntry(email);
    }
}
