using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.ListCoupons;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Coupons.ListCoupons;

[TestClass]
public sealed class ListCouponsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an event with both an active and a revoked coupon
    // When the coupons are listed for the event
    // Then both coupons are returned with their correct status
    [TestMethod]
    public async ValueTask ListCoupons_MultipleCouponStates_ReturnsAllWithCorrectStatus()
    {
        // Arrange
        var fixture = ListCouponsFixture.WithCoupons();
        await fixture.SetupAsync(Environment);

        var query = new ListCouponsQuery(fixture.EventId, fixture.TeamId);
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

    // Given an event with no coupons
    // When the coupons are listed for the event
    // Then an empty list is returned
    [TestMethod]
    public async ValueTask ListCoupons_NoCouponsExist_ReturnsEmptyList()
    {
        // Arrange
        var fixture = ListCouponsFixture.EmptyList();
        await fixture.SetupAsync(Environment);

        var query = new ListCouponsQuery(fixture.EventId, fixture.TeamId);
        var sut = NewListCouponsHandler();

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Coupons.ShouldBeEmpty();
    }

    // Given an event with coupons created by an organiser and coupons generated from the waitlist
    // When the coupons are listed for the event
    // Then each coupon is returned with its correct source
    [TestMethod]
    public async ValueTask ListCoupons_MixedSources_ReturnsCorrectSourceForEach()
    {
        // Arrange
        var fixture = ListCouponsFixture.WithMixedSources();
        await fixture.SetupAsync(Environment);

        var query = new ListCouponsQuery(fixture.EventId, fixture.TeamId);
        var sut = NewListCouponsHandler();

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Coupons.Count.ShouldBe(2);

        var organiser = result.Coupons.SingleOrDefault(c => c.Email == "organiser@example.com");
        organiser.ShouldNotBeNull().Source.ShouldBe(CouponSource.Organiser);

        var waitlist = result.Coupons.SingleOrDefault(c => c.Email == "waitlist@example.com");
        waitlist.ShouldNotBeNull().Source.ShouldBe(CouponSource.Waitlist);
    }

    private static ListCouponsHandler NewListCouponsHandler() =>
        new(Environment.RegistrationsDatabase.Context);
}
