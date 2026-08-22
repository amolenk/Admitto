using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.RegisterAttendee;

[TestClass]
public sealed class RegisterWithCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Successful coupon registration — capacity exceeded, still registers and increments used
    // Given a coupon for a ticket type whose capacity is already exceeded
    // When an attendee registers using the coupon
    // Then the registration succeeds, the coupon is redeemed, and used capacity is incremented
    [TestMethod]
    public async ValueTask RegisterWithCoupon_CapacityExceeded_SucceedsAndIncrementsUsedCapacity()
    {
        var fixture = RegisterAttendeeFixture.CouponHappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.Value.ShouldBe(registrationId);
            registration.Email.ShouldBe(fixture.CouponEmail);

            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RedeemedAt.ShouldNotBeNull();

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(6);
        });
    }

    // Coupon rejected — expired
    // Given an expired coupon
    // When an attendee registers using the coupon
    // Then it fails with a coupon-expired error
    [TestMethod]
    public async ValueTask RegisterWithCoupon_ExpiredCoupon_ThrowsCouponExpiredError()
    {
        var fixture = RegisterAttendeeFixture.CouponExpired();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Coupon.Errors.Expired);
    }

    // Coupon rejected — already redeemed
    // Given a coupon that has already been redeemed
    // When an attendee registers using the coupon
    // Then it fails with a coupon-already-redeemed error
    [TestMethod]
    public async ValueTask RegisterWithCoupon_AlreadyRedeemed_ThrowsCouponAlreadyRedeemedError()
    {
        var fixture = RegisterAttendeeFixture.CouponRedeemed();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Coupon.Errors.AlreadyRedeemed);
    }

    // Coupon rejected — revoked
    // Given a coupon that has been revoked
    // When an attendee registers using the coupon
    // Then it fails with a coupon-revoked error
    [TestMethod]
    public async ValueTask RegisterWithCoupon_RevokedCoupon_ThrowsCouponRevokedError()
    {
        var fixture = RegisterAttendeeFixture.CouponRevoked();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Coupon.Errors.Revoked);
    }

    // Coupon rejected — ticket type not allowlisted
    // Given a coupon that only allows a specific ticket type
    // When an attendee registers requesting a different ticket type
    // Then it fails with a ticket-type-not-allowed error
    [TestMethod]
    public async ValueTask RegisterWithCoupon_TicketTypeNotAllowlisted_ThrowsNotAllowlistedError()
    {
        var fixture = RegisterAttendeeFixture.CouponTicketTypeNotAllowlisted();
        await fixture.SetupAsync(Environment);

        // Requesting "general-admission" but coupon only allows "speaker-pass".
        var command = new RegisterAttendeeWithCouponCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.CouponEmail.Value,
            "Coupon",
            "User",
            [fixture.GetTicketTypeId("general-admission").Value],
            CouponCode: fixture.CouponCode);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(
            Coupon.Errors.TicketTypeNotAllowlisted([fixture.GetTicketTypeId("general-admission").Value]));
    }

    // Coupon bypasses registration window when flag set
    // Given a coupon configured to bypass the registration window and a closed registration window
    // When an attendee registers using the coupon
    // Then the registration succeeds
    [TestMethod]
    public async ValueTask RegisterWithCoupon_BypassesClosedWindow_Succeeds()
    {
        var fixture = RegisterAttendeeFixture.CouponBypassesClosedWindow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
        });
    }

    // Coupon respects registration window when flag not set
    // Given a coupon that does not bypass the registration window and a closed registration window
    // When an attendee registers using the coupon
    // Then it fails with a registration-closed error
    [TestMethod]
    public async ValueTask RegisterWithCoupon_RespectsClosedWindow_ThrowsRegistrationClosed()
    {
        var fixture = RegisterAttendeeFixture.CouponRespectsClosedWindow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEvent.Errors.RegistrationClosed);
    }

    // Coupon bypasses domain restriction (target email outside allowed domain)
    // Given a coupon whose target email is outside the event's allowed email domain
    // When an attendee registers using the coupon
    // Then the registration succeeds with the coupon's email
    [TestMethod]
    public async ValueTask RegisterWithCoupon_BypassesDomainRestriction_Succeeds()
    {
        var fixture = RegisterAttendeeFixture.CouponBypassesDomainRestriction();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Email.ShouldBe(fixture.CouponEmail);
        });
    }

    // Coupon bypasses capacity requirement (null MaxCapacity)
    // Given a ticket type with no maximum capacity configured
    // When an attendee registers using a coupon for that ticket type
    // Then the registration succeeds and used capacity is incremented
    [TestMethod]
    public async ValueTask RegisterWithCoupon_NullCapacity_SucceedsAndIncrementsUsedCapacity()
    {
        var fixture = RegisterAttendeeFixture.CouponBypassesNullCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(1);
            catalog.TicketTypes[0].MaxCapacity.ShouldBeNull();
        });
    }

    // Coupon does not bypass archived event — active-status gate still applies
    // Given an archived ticketed event
    // When an attendee registers using a valid coupon
    // Then it fails with an event-not-active error
    [TestMethod]
    public async ValueTask RegisterWithCoupon_EventArchived_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.CouponEventArchived();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterAttendeeWithCouponHandler.Errors.EventNotActive);
    }

    // Coupon rejected — supplied email does not match coupon target email
    // Given a coupon tied to a specific target email
    // When an attendee registers with a different email
    // Then it fails with a coupon-email-mismatch error
    [TestMethod]
    public async ValueTask RegisterWithCoupon_EmailMismatch_ThrowsCouponEmailMismatch()
    {
        var fixture = RegisterAttendeeFixture.CouponHappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "imposter@example.com");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Coupon.Errors.EmailMismatch);
    }

    // Coupon mode does NOT require an email-verification token
    // Given a valid coupon and no email-verification token supplied
    // When an attendee registers using the coupon
    // Then the registration succeeds
    [TestMethod]
    public async ValueTask RegisterWithCoupon_NoTokenRequired_Succeeds()
    {
        var fixture = RegisterAttendeeFixture.CouponHappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
        });
    }

    // Coupon registration resets a cancelled registration
    // Given a cancelled existing registration with previously stored additional details
    // When the same attendee re-registers using a coupon with new additional details
    // Then the existing registration is reset to Registered with the new details and the coupon is redeemed
    [TestMethod]
    public async ValueTask RegisterWithCoupon_CancelledRegistration_ResetsExistingRegistration()
    {
        var fixture = RegisterAttendeeFixture.CouponHappyFlow();
        fixture
            .ConfigureAdditionalDetailSchema(("badge", "Badge", 20))
            .WithCancelledExistingRegistration(
                email: fixture.CouponEmail.Value,
                additionalDetails: new Dictionary<string, string> { ["badge"] = "old" });
        await fixture.SetupAsync(Environment);

        var command = NewCommand(
            fixture,
            fixture.CouponEmail.Value,
            new Dictionary<string, string> { ["badge"] = "speaker" });
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        registrationId.ShouldBe(fixture.ExistingRegistrationId.Value);
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleAsync(testContext.CancellationToken);
            registration.Id.ShouldBe(fixture.ExistingRegistrationId);
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Email.ShouldBe(fixture.CouponEmail);
            registration.FirstName.ShouldBe(FirstName.From("Test"));
            registration.LastName.ShouldBe(LastName.From("User"));
            registration.CancellationReason.ShouldBeNull();
            registration.HasReconfirmed.ShouldBeFalse();
            registration.ReconfirmedAt.ShouldBeNull();
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.TicketTypeId);
            registration.AdditionalDetails["badge"].ShouldBe("speaker");
            AssertAttendeeRegisteredEvent(registration);

            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldNotBeNull();

            var catalog = await dbContext.TicketCatalogs.SingleAsync(testContext.CancellationToken);
            catalog.TicketTypes.Single(tt => tt.Id == fixture.TicketTypeId).UsedCapacity.ShouldBe(6);
        });
    }

    // Waitlist coupon — marks WaitlistCoupon as Redeemed in same transaction
    // Given a coupon issued from the waitlist
    // When an attendee registers using the coupon
    // Then the coupon and the corresponding waitlist coupon are both marked redeemed
    [TestMethod]
    public async ValueTask RegisterWithCoupon_WaitlistCoupon_MarksWaitlistCouponRedeemed()
    {
        var fixture = RegisterAttendeeFixture.WaitlistCouponHappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldNotBeNull();

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Redeemed);
        });
    }

    // Organiser coupon with WaitlistMode active — Waitlist is not touched
    // Given an organiser-issued coupon while the event's waitlist mode is active
    // When an attendee registers using the coupon
    // Then the coupon is redeemed but the waitlist is left untouched
    [TestMethod]
    public async ValueTask RegisterWithCoupon_OrganiserCouponWithWaitlistActive_DoesNotTouchWaitlist()
    {
        var fixture = RegisterAttendeeFixture.OrganiserCouponWithWaitlistActive();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldNotBeNull();

            var waitlist = await dbContext.Waitlists.SingleAsync(testContext.CancellationToken);
            waitlist.Coupons.ShouldBeEmpty();
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RegisterAttendeeWithCouponCommand NewCommand(RegisterAttendeeFixture fixture, string email)
        => new(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            email,
            "Test",
            "User",
            [fixture.TicketTypeId.Value],
            CouponCode: fixture.CouponCode);

    private static RegisterAttendeeWithCouponCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        IReadOnlyDictionary<string, string>? additionalDetails)
        => new(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            email,
            "Test",
            "User",
            [fixture.TicketTypeId.Value],
            CouponCode: fixture.CouponCode,
            AdditionalDetails: additionalDetails);

    private static void AssertAttendeeRegisteredEvent(Registration registration)
    {
        var domainEvent = registration.GetDomainEvents()
            .OfType<AttendeeRegisteredDomainEvent>()
            .ShouldHaveSingleItem();
        domainEvent.RegistrationId.ShouldBe(registration.Id);
        domainEvent.RecipientEmail.ShouldBe(registration.Email);
        domainEvent.FirstName.ShouldBe(registration.FirstName);
        domainEvent.LastName.ShouldBe(registration.LastName);
        domainEvent.Tickets.ShouldBe(registration.Tickets);
    }

    private static RegisterAttendeeWithCouponHandler NewHandler()
        => new(Environment.RegistrationsDatabase.Context, TimeProvider.System);
}
