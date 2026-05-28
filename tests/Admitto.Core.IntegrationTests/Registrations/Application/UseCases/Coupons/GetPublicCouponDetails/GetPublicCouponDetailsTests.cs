using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetPublicCouponDetails;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Coupons.GetPublicCouponDetails;

[TestClass]
public sealed class GetPublicCouponDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask GetPublicCouponDetails_ActiveCoupon_ReturnsStatusAndTicketTypes()
    {
        // Arrange
        var fixture = GetPublicCouponDetailsFixture.WithActiveCoupon();
        await fixture.SetupAsync(Environment);

        var query = new GetPublicCouponDetailsQuery(fixture.EventId, fixture.CouponCode);
        var sut = NewHandler();

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(CouponStatus.Active),
            () => result.AllowedTicketTypes.Count.ShouldBe(1),
            () => result.AllowedTicketTypes[0].Id.ShouldBe(fixture.TicketTypeId.Value),
            () => result.AllowedTicketTypes[0].Name.ShouldBe(fixture.TicketTypeName),
            () => result.ExpiresAt.ShouldNotBeNull());
    }

    [TestMethod]
    public async ValueTask GetPublicCouponDetails_RedeemedCoupon_ReturnsRedeemedStatus()
    {
        // Arrange
        var fixture = GetPublicCouponDetailsFixture.WithRedeemedCoupon();
        await fixture.SetupAsync(Environment);

        var query = new GetPublicCouponDetailsQuery(fixture.EventId, fixture.CouponCode);
        var sut = NewHandler();

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Status.ShouldBe(CouponStatus.Redeemed);
    }

    [TestMethod]
    public async ValueTask GetPublicCouponDetails_NonExistentCouponCode_ThrowsNotFoundError()
    {
        // Arrange
        var fixture = GetPublicCouponDetailsFixture.NoCoupon();
        await fixture.SetupAsync(Environment);

        var query = new GetPublicCouponDetailsQuery(fixture.EventId, CouponCode.New());
        var sut = NewHandler();

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(query, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(NotFoundError.Create<Coupon>());
    }

    [TestMethod]
    public async ValueTask GetPublicCouponDetails_CouponBelongsToOtherEvent_ThrowsNotFoundError()
    {
        // Arrange
        var fixture = GetPublicCouponDetailsFixture.WithActiveCoupon();
        await fixture.SetupAsync(Environment);

        // Use a different EventId than the one the coupon belongs to
        var otherEventId = TicketedEventId.New();
        var query = new GetPublicCouponDetailsQuery(otherEventId, fixture.CouponCode);
        var sut = NewHandler();

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(query, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(NotFoundError.Create<Coupon>());
    }

    private static GetPublicCouponDetailsHandler NewHandler() =>
        new(Environment.RegistrationsDatabase.Context);
}
