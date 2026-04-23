using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterAttendee;

[TestClass]
public sealed class RegisterWithCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Successful coupon registration — capacity exceeded, still registers and increments used
    [TestMethod]
    public async ValueTask SC001_RegisterWithCoupon_CapacityExceeded_SucceedsAndIncrementsUsedCapacity()
    {
        var fixture = RegisterAttendeeFixture.CouponHappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.ShouldBe(registrationId);
            registration.Email.ShouldBe(fixture.CouponEmail);

            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RedeemedAt.ShouldNotBeNull();

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(6);
        });
    }

    // SC002: Coupon rejected — expired
    [TestMethod]
    public async ValueTask SC002_RegisterWithCoupon_ExpiredCoupon_ThrowsCouponExpiredError()
    {
        var fixture = RegisterAttendeeFixture.CouponExpired();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterAttendeeHandler.Errors.CouponExpired);
    }

    // SC003: Coupon rejected — already redeemed
    [TestMethod]
    public async ValueTask SC003_RegisterWithCoupon_AlreadyRedeemed_ThrowsCouponAlreadyRedeemedError()
    {
        var fixture = RegisterAttendeeFixture.CouponRedeemed();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterAttendeeHandler.Errors.CouponAlreadyRedeemed);
    }

    // SC004: Coupon rejected — revoked
    [TestMethod]
    public async ValueTask SC004_RegisterWithCoupon_RevokedCoupon_ThrowsCouponRevokedError()
    {
        var fixture = RegisterAttendeeFixture.CouponRevoked();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterAttendeeHandler.Errors.CouponRevoked);
    }

    // SC005: Coupon rejected — ticket type not allowlisted
    [TestMethod]
    public async ValueTask SC005_RegisterWithCoupon_TicketTypeNotAllowlisted_ThrowsNotAllowlistedError()
    {
        var fixture = RegisterAttendeeFixture.CouponTicketTypeNotAllowlisted();
        await fixture.SetupAsync(Environment);

        // Requesting "general-admission" but coupon only allows "speaker-pass".
        var command = new RegisterAttendeeCommand(
            fixture.EventId,
            fixture.CouponEmail,
            ["general-admission"],
            RegistrationMode.Coupon,
            CouponCode: fixture.CouponCodeString);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("coupon.ticket_type_not_allowed");
    }

    // SC006: Coupon bypasses registration window when flag set
    [TestMethod]
    public async ValueTask SC006_RegisterWithCoupon_BypassesClosedWindow_Succeeds()
    {
        var fixture = RegisterAttendeeFixture.CouponBypassesClosedWindow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
        });
    }

    // SC007: Coupon respects registration window when flag not set
    [TestMethod]
    public async ValueTask SC007_RegisterWithCoupon_RespectsClosedWindow_ThrowsRegistrationClosed()
    {
        var fixture = RegisterAttendeeFixture.CouponRespectsClosedWindow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.closed");
    }

    // SC008: Coupon bypasses domain restriction (target email outside allowed domain)
    [TestMethod]
    public async ValueTask SC008_RegisterWithCoupon_BypassesDomainRestriction_Succeeds()
    {
        var fixture = RegisterAttendeeFixture.CouponBypassesDomainRestriction();
        await fixture.SetupAsync(Environment);

        // The coupon is bound to fixture.CouponEmail (gmail.com), event allows only @acme.com.
        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Email.ShouldBe(fixture.CouponEmail);
        });
    }

    // SC009: Coupon bypasses capacity requirement (null MaxCapacity)
    [TestMethod]
    public async ValueTask SC009_RegisterWithCoupon_NullCapacity_SucceedsAndIncrementsUsedCapacity()
    {
        var fixture = RegisterAttendeeFixture.CouponBypassesNullCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(1);
            catalog.TicketTypes[0].MaxCapacity.ShouldBeNull();
        });
    }

    // SC010: Coupon does not bypass cancelled event — active-status gate still applies
    [TestMethod]
    public async ValueTask SC010_RegisterWithCoupon_EventCancelled_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.CouponEventCancelled();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // SC011: Coupon rejected — supplied email does not match coupon target email (D8)
    [TestMethod]
    public async ValueTask SC011_RegisterWithCoupon_EmailMismatch_ThrowsCouponEmailMismatch()
    {
        var fixture = RegisterAttendeeFixture.CouponHappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "imposter@example.com");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("coupon.email_mismatch");
    }

    // SC012: Coupon mode does NOT require an email-verification token
    [TestMethod]
    public async ValueTask SC012_RegisterWithCoupon_NoTokenRequired_Succeeds()
    {
        var fixture = RegisterAttendeeFixture.CouponHappyFlow();
        await fixture.SetupAsync(Environment);

        // Token deliberately omitted; coupon mode must not invoke the verifier.
        var command = NewCommand(fixture, fixture.CouponEmail.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RegisterAttendeeCommand NewCommand(RegisterAttendeeFixture fixture, string email)
        => new(
            fixture.EventId,
            EmailAddress.From(email),
            [fixture.TicketTypeSlug],
            RegistrationMode.Coupon,
            CouponCode: fixture.CouponCodeString);

    private static RegisterAttendeeHandler NewHandler()
        => new(Environment.Database.Context, TimeProvider.System, new StubEmailVerificationTokenValidator());
}
