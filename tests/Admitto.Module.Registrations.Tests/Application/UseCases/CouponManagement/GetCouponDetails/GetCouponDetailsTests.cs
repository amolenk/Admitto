using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CouponManagement.GetCouponDetails;

[TestClass]
public sealed class GetCouponDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-009: View coupon details
    [TestMethod]
    public async ValueTask SC009_GetCouponDetails_ExistingCoupon_ReturnsFullDetailsIncludingCode()
    {
        // Arrange
        var fixture = GetCouponDetailsFixture.WithCoupon();
        await fixture.SetupAsync(Environment);

        var query = new GetCouponDetailsQuery(fixture.EventId, fixture.CouponId);
        var sut = NewGetCouponDetailsHandler();

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBe(fixture.CouponId.Value),
            () => result.Code.ShouldNotBe(Guid.Empty),
            () => result.Email.ShouldBe("speaker@example.com"),
            () => result.Status.ShouldBe(CouponStatus.Active),
            () => result.AllowedTicketTypeIds.ShouldContain(fixture.TicketTypeId.Value),
            () => result.BypassRegistrationWindow.ShouldBeTrue(),
            () => result.RedeemedAt.ShouldBeNull(),
            () => result.RevokedAt.ShouldBeNull());
    }

    [TestMethod]
    public async ValueTask GetCouponDetails_NonExistentCoupon_ThrowsNotFoundError()
    {
        // Arrange
        var fixture = GetCouponDetailsFixture.NoCoupon();
        await fixture.SetupAsync(Environment);

        var query = new GetCouponDetailsQuery(fixture.EventId, fixture.CouponId);
        var sut = NewGetCouponDetailsHandler();

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(query, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(NotFoundError.Create<Coupon>(fixture.CouponId.Value));
    }

    private static GetCouponDetailsHandler NewGetCouponDetailsHandler() =>
        new(Environment.Database.Context);
}
