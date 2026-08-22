using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.CreateCoupon;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Coupons.CreateCoupon;

[TestClass]
public sealed class CreateCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a ticketed event with a valid ticket type
    // When a valid CreateCoupon command is handled
    // Then a coupon is persisted with the given details and no registration-window bypass
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

    // Given a ticketed event with a valid ticket type
    // When a CreateCoupon command with the bypass-registration-window flag set is handled
    // Then the persisted coupon has the bypass flag set
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

    // Given a ticketed event
    // When a CreateCoupon command references a ticket type id that does not exist
    // Then an unknown-ticket-types error is thrown
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

    // Given a ticketed event with a valid ticket type
    // When a CreateCoupon command specifies an expiry date in the past
    // Then an expiry-must-be-in-future error is thrown
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

    // NOTE: cancelled-event rejection will be reintroduced against the new TicketedEvent
    // aggregate in section 8 of redesign-ticketed-event-ownership.

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
