using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CouponManagement.CreateCoupon;

[TestClass]
public sealed class CreateCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Successful coupon creation
    [TestMethod]
    public async ValueTask SC001_CreateCoupon_ValidInput_PersistsCouponAndRaisesDomainEvent()
    {
        // Arrange
        var fixture = CreateCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCreateCouponCommand(
            fixture.EventId,
            allowedTicketTypeSlugs: [fixture.TicketTypeSlug]);
        var sut = NewCreateCouponHandler(fixture);

        // Act
        var couponId = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);

            coupon.ShouldNotBeNull();
            coupon.Id.ShouldBe(couponId);
            coupon.EventId.ShouldBe(fixture.EventId);
            coupon.Email.Value.ShouldBe("speaker@example.com");
            coupon.AllowedTicketTypeSlugs.ShouldContain(fixture.TicketTypeSlug);
            coupon.Code.Value.ShouldNotBe(Guid.Empty);
            coupon.BypassRegistrationWindow.ShouldBeFalse();
        });
    }

    // SC-002: Coupon with registration window bypass
    [TestMethod]
    public async ValueTask SC002_CreateCoupon_BypassRegistrationWindow_PersistsWithBypassFlag()
    {
        // Arrange
        var fixture = CreateCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCreateCouponCommand(
            fixture.EventId,
            allowedTicketTypeSlugs: [fixture.TicketTypeSlug],
            bypassRegistrationWindow: true);
        var sut = NewCreateCouponHandler(fixture);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().BypassRegistrationWindow.ShouldBeTrue();
        });
    }

    // SC-003: Rejected — ticket type does not exist
    [TestMethod]
    public async ValueTask SC003_CreateCoupon_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        // Arrange
        var fixture = CreateCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var unknownSlug = "unknown-type";
        var command = NewCreateCouponCommand(
            fixture.EventId,
            allowedTicketTypeSlugs: [unknownSlug]);
        var sut = NewCreateCouponHandler(fixture);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.UnknownTicketTypes([unknownSlug]));
    }

    // SC-004: Rejected — ticket type is cancelled
    [TestMethod]
    public async ValueTask SC004_CreateCoupon_CancelledTicketType_ThrowsCancelledTicketTypesError()
    {
        // Arrange
        var fixture = CreateCouponFixture.WithCancelledTicketType();
        await fixture.SetupAsync(Environment);

        var command = NewCreateCouponCommand(
            fixture.EventId,
            allowedTicketTypeSlugs: [fixture.CancelledTicketTypeSlug]);
        var sut = NewCreateCouponHandler(fixture);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.CancelledTicketTypes([fixture.CancelledTicketTypeSlug]));
    }

    // SC-005: Rejected — expiry in the past
    [TestMethod]
    public async ValueTask SC005_CreateCoupon_ExpiryInThePast_ThrowsExpiryMustBeInFutureError()
    {
        // Arrange
        var fixture = CreateCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCreateCouponCommand(
            fixture.EventId,
            allowedTicketTypeSlugs: [fixture.TicketTypeSlug],
            expiresAt: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var sut = NewCreateCouponHandler(fixture);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.ExpiryMustBeInFuture);
    }

    // SC-006: Rejected — cancelled event
    [TestMethod]
    public async ValueTask SC006_CreateCoupon_CancelledEvent_ThrowsEventNotActiveError()
    {
        // Arrange
        var fixture = CreateCouponFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = NewCreateCouponCommand(
            fixture.EventId,
            allowedTicketTypeSlugs: [fixture.TicketTypeSlug]);
        var sut = NewCreateCouponHandler(fixture);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }

    private static CreateCouponCommand NewCreateCouponCommand(
        TicketedEventId eventId,
        string[]? allowedTicketTypeSlugs = null,
        string? email = null,
        DateTimeOffset? expiresAt = null,
        bool bypassRegistrationWindow = false)
    {
        email ??= "speaker@example.com";
        expiresAt ??= DateTimeOffset.UtcNow.AddDays(30);
        allowedTicketTypeSlugs ??= ["general-admission"];

        return new CreateCouponCommand(
            eventId,
            EmailAddress.From(email),
            allowedTicketTypeSlugs,
            expiresAt.Value,
            bypassRegistrationWindow);
    }

    private static CreateCouponHandler NewCreateCouponHandler(CreateCouponFixture fixture) =>
        new(Environment.Database.Context, TimeProvider.System);
}
