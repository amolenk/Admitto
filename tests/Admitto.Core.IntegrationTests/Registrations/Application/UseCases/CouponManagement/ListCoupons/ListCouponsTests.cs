using Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.ListCoupons;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.CouponManagement.ListCoupons;

[TestClass]
public sealed class ListCouponsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-007: List coupons for an event
    [TestMethod]
    public async ValueTask ListCoupons_MultipleCouponStates_ReturnsAllWithCorrectStatus()
    {
        // Arrange
        var fixture = ListCouponsFixture.WithCoupons();
        await fixture.SetupAsync(Environment);

        var query = new ListCouponsQuery(fixture.EventId);
        var sut = NewListCouponsHandler();

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Coupons.Count.ShouldBe(2);

        var active = result.Coupons.SingleOrDefault(c => c.Email == "active@example.com");
        active.ShouldNotBeNull().Status.ShouldBe(CouponStatus.Active);

        var revoked = result.Coupons.SingleOrDefault(c => c.Email == "revoked@example.com");
        revoked.ShouldNotBeNull().Status.ShouldBe(CouponStatus.Revoked);
    }

    // SC-008: Empty coupon list
    [TestMethod]
    public async ValueTask ListCoupons_NoCouponsExist_ReturnsEmptyList()
    {
        // Arrange
        var fixture = ListCouponsFixture.EmptyList();
        await fixture.SetupAsync(Environment);

        var query = new ListCouponsQuery(fixture.EventId);
        var sut = NewListCouponsHandler();

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Coupons.ShouldBeEmpty();
    }

    private static ListCouponsHandler NewListCouponsHandler() =>
        new(Environment.RegistrationsDatabase.Context);
}
