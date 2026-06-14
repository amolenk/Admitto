using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.CreateCoupon;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Coupons.CreateCoupon;

[TestClass]
public sealed class CreateCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Successful coupon creation
    [TestMethod]
    public async ValueTask CreateCoupon_ValidInput_PersistsCouponAndRaisesDomainEvent()
    {
        // Arrange
        var fixture = CreateCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCreateCouponCommand(
            fixture.EventId,
            fixture.TeamId.Value,
            allowedTicketTypeIds: [fixture.TicketTypeId.Value]);
        var sut = NewCreateCouponHandler(fixture);

        // Act
        var couponId = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);

            coupon.ShouldNotBeNull();
            coupon.Id.Value.ShouldBe(couponId);
            coupon.EventId.ShouldBe(fixture.EventId);
            coupon.Email.Value.ShouldBe("speaker@example.com");
            coupon.AllowedTicketTypeIds.ShouldContain(fixture.TicketTypeId);
            coupon.Code.Value.ShouldNotBe(Guid.Empty);
            coupon.BypassRegistrationWindow.ShouldBeFalse();
        });
    }

    // SC-002: Coupon with registration window bypass
    [TestMethod]
    public async ValueTask CreateCoupon_BypassRegistrationWindow_PersistsWithBypassFlag()
    {
        // Arrange
        var fixture = CreateCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCreateCouponCommand(
            fixture.EventId,
            fixture.TeamId.Value,
            allowedTicketTypeIds: [fixture.TicketTypeId.Value],
            bypassRegistrationWindow: true);
        var sut = NewCreateCouponHandler(fixture);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().BypassRegistrationWindow.ShouldBeTrue();
        });
    }

    // SC-003: Rejected — ticket type does not exist
    [TestMethod]
    public async ValueTask CreateCoupon_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        // Arrange
        var fixture = CreateCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var unknownId = Guid.NewGuid();
        var command = NewCreateCouponCommand(
            fixture.EventId,
            fixture.TeamId.Value,
            allowedTicketTypeIds: [unknownId]);
        var sut = NewCreateCouponHandler(fixture);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.UnknownTicketTypes([unknownId]));
    }

    // SC-005: Rejected — expiry in the past
    [TestMethod]
    public async ValueTask CreateCoupon_ExpiryInThePast_ThrowsExpiryMustBeInFutureError()
    {
        // Arrange
        var fixture = CreateCouponFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewCreateCouponCommand(
            fixture.EventId,
            fixture.TeamId.Value,
            allowedTicketTypeIds: [fixture.TicketTypeId.Value],
            expiresAt: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var sut = NewCreateCouponHandler(fixture);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.ExpiryMustBeInFuture);
    }

    // NOTE: SC-006 (cancelled-event rejection) will be reintroduced against the new
    // TicketedEvent aggregate in section 8 of redesign-ticketed-event-ownership.

    private static CreateCouponCommand NewCreateCouponCommand(
        TicketedEventId eventId,
        Guid teamId = default,
        Guid[]? allowedTicketTypeIds = null,
        string? email = null,
        DateTimeOffset? expiresAt = null,
        bool bypassRegistrationWindow = false)
    {
        email ??= "speaker@example.com";
        expiresAt ??= DateTimeOffset.UtcNow.AddDays(30);
        allowedTicketTypeIds ??= [Guid.NewGuid()];

        return new CreateCouponCommand(
            teamId == default ? Guid.NewGuid() : teamId,
            eventId.Value,
            email,
            allowedTicketTypeIds,
            expiresAt.Value,
            bypassRegistrationWindow);
    }

    private static CreateCouponHandler NewCreateCouponHandler(CreateCouponFixture fixture) =>
        new(Environment.RegistrationsDatabase.Context, TimeProvider.System);
}
