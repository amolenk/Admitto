using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Tests.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.RegisterAttendee;

[TestClass]
public sealed class RegisterWithCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Successful coupon registration — capacity exceeded, still registers and increments used
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
    [TestMethod]
    public async ValueTask RegisterWithCoupon_ExpiredCoupon_ThrowsCouponExpiredError()
    {
        var fixture = RegisterAttendeeFixture.CouponExpired();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterAttendeeHandler.Errors.CouponExpired);
    }

    // Coupon rejected — already redeemed
    [TestMethod]
    public async ValueTask RegisterWithCoupon_AlreadyRedeemed_ThrowsCouponAlreadyRedeemedError()
    {
        var fixture = RegisterAttendeeFixture.CouponRedeemed();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterAttendeeHandler.Errors.CouponAlreadyRedeemed);
    }

    // Coupon rejected — revoked
    [TestMethod]
    public async ValueTask RegisterWithCoupon_RevokedCoupon_ThrowsCouponRevokedError()
    {
        var fixture = RegisterAttendeeFixture.CouponRevoked();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterAttendeeHandler.Errors.CouponRevoked);
    }

    // Coupon rejected — ticket type not allowlisted
    [TestMethod]
    public async ValueTask RegisterWithCoupon_TicketTypeNotAllowlisted_ThrowsNotAllowlistedError()
    {
        var fixture = RegisterAttendeeFixture.CouponTicketTypeNotAllowlisted();
        await fixture.SetupAsync(Environment);

        // Requesting "general-admission" but coupon only allows "speaker-pass".
        var command = new RegisterAttendeeCommand(
            fixture.EventId.Value,
            fixture.CouponEmail.Value,
            "Coupon",
            "User",
            [fixture.GetTicketTypeId("general-admission").Value],
            RegistrationMode.Coupon,
            CouponCode: fixture.CouponCodeString);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("coupon.ticket_type_not_allowed");
    }

    // Coupon bypasses registration window when flag set
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
    [TestMethod]
    public async ValueTask RegisterWithCoupon_RespectsClosedWindow_ThrowsRegistrationClosed()
    {
        var fixture = RegisterAttendeeFixture.CouponRespectsClosedWindow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.closed");
    }

    // Coupon bypasses domain restriction (target email outside allowed domain)
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
    [TestMethod]
    public async ValueTask RegisterWithCoupon_EventArchived_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.CouponEventArchived();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // Coupon rejected — supplied email does not match coupon target email
    [TestMethod]
    public async ValueTask RegisterWithCoupon_EmailMismatch_ThrowsCouponEmailMismatch()
    {
        var fixture = RegisterAttendeeFixture.CouponHappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "imposter@example.com");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("coupon.email_mismatch");
    }

    // Coupon mode does NOT require an email-verification token
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RegisterAttendeeCommand NewCommand(RegisterAttendeeFixture fixture, string email)
        => new(
            fixture.EventId.Value,
            email,
            "Test",
            "User",
            [fixture.TicketTypeId.Value],
            RegistrationMode.Coupon,
            CouponCode: fixture.CouponCodeString);

    private static RegisterAttendeeCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        IReadOnlyDictionary<string, string>? additionalDetails)
        => new(
            fixture.EventId.Value,
            email,
            "Test",
            "User",
            [fixture.TicketTypeId.Value],
            RegistrationMode.Coupon,
            CouponCode: fixture.CouponCodeString,
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

    private static RegisterAttendeeHandler NewHandler()
        => new(Environment.RegistrationsDatabase.Context, TimeProvider.System, new StubEmailVerificationTokenValidator());
}
