using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterWithCoupon;

[TestClass]
public sealed class RegisterWithCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Successful coupon registration — capacity exceeded, still registers and increments used
    [TestMethod]
    public async ValueTask SC001_RegisterWithCoupon_CapacityExceeded_SucceedsAndIncrementsUsedCapacity()
    {
        var fixture = RegisterWithCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@gmail.com");
        var sut = NewHandler(fixture);

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.ShouldBe(registrationId);
            registration.Email.Value.ShouldBe("speaker@gmail.com");

            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RedeemedAt.ShouldNotBeNull();

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(6); // was 5/5, now 6
        });
    }

    // SC002: Coupon rejected — expired
    [TestMethod]
    public async ValueTask SC002_RegisterWithCoupon_ExpiredCoupon_ThrowsCouponExpiredError()
    {
        var fixture = RegisterWithCouponFixture.ExpiredCoupon();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "attendee@example.com");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterWithCouponHandler.Errors.CouponExpired);
    }

    // SC003: Coupon rejected — already redeemed
    [TestMethod]
    public async ValueTask SC003_RegisterWithCoupon_AlreadyRedeemed_ThrowsCouponAlreadyRedeemedError()
    {
        var fixture = RegisterWithCouponFixture.RedeemedCoupon();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "attendee@example.com");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterWithCouponHandler.Errors.CouponAlreadyRedeemed);
    }

    // SC004: Coupon rejected — revoked
    [TestMethod]
    public async ValueTask SC004_RegisterWithCoupon_RevokedCoupon_ThrowsCouponRevokedError()
    {
        var fixture = RegisterWithCouponFixture.RevokedCoupon();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "attendee@example.com");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterWithCouponHandler.Errors.CouponRevoked);
    }

    // SC005: Coupon rejected — ticket type not allowlisted
    [TestMethod]
    public async ValueTask SC005_RegisterWithCoupon_TicketTypeNotAllowlisted_ThrowsNotAllowlistedError()
    {
        var fixture = RegisterWithCouponFixture.TicketTypeNotAllowlisted();
        await fixture.SetupAsync(Environment);

        // Requesting "general-admission" but coupon only allows "speaker-pass".
        var command = new RegisterWithCouponCommand(
            fixture.EventId,
            fixture.CouponCodeString,
            EmailAddress.From("attendee@example.com"),
            ["general-admission"]);
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("coupon.ticket_type_not_allowed");
    }

    // SC006: Coupon bypasses registration window when flag set
    [TestMethod]
    public async ValueTask SC006_RegisterWithCoupon_BypassWindowFlag_SucceedsEvenWhenWindowClosed()
    {
        var fixture = RegisterWithCouponFixture.BypassesWindow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@gmail.com");
        var sut = NewHandler(fixture);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
        });
    }

    // SC007: Coupon respects registration window when flag not set
    [TestMethod]
    public async ValueTask SC007_RegisterWithCoupon_NoBypassFlag_ThrowsRegistrationClosedWhenWindowClosed()
    {
        var fixture = RegisterWithCouponFixture.RespectsWindow();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@gmail.com");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterWithCouponHandler.Errors.RegistrationClosed);
    }

    // SC008: Coupon bypasses domain restriction
    [TestMethod]
    public async ValueTask SC008_RegisterWithCoupon_BypassesDomainRestriction_SucceedsWithAnyEmail()
    {
        var fixture = RegisterWithCouponFixture.BypassesDomainRestriction();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "external@gmail.com");
        var sut = NewHandler(fixture);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Email.Value.ShouldBe("external@gmail.com");
        });
    }

    // SC009: Coupon bypasses capacity requirement (null MaxCapacity)
    [TestMethod]
    public async ValueTask SC009_RegisterWithCoupon_NullCapacity_SucceedsAndIncrementsUsedCapacity()
    {
        var fixture = RegisterWithCouponFixture.BypassesNullCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@gmail.com");
        var sut = NewHandler(fixture);

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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RegisterWithCouponCommand NewCommand(RegisterWithCouponFixture fixture, string email)
        => new(fixture.EventId, fixture.CouponCodeString, EmailAddress.From(email), [fixture.TicketTypeSlug]);

    private static RegisterWithCouponHandler NewHandler(RegisterWithCouponFixture fixture)
        => new(Environment.Database.Context, TimeProvider.System);
}
