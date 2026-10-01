using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.Coupons;

// Coupon endpoints require the Organizer team role; a Crew member or a member of a different
// team must be rejected, and an anonymous caller must be rejected entirely.
[TestClass]
public sealed class CouponAuthorizationTests(TestContext testContext) : EndToEndTestBase
{
    private static object NewCouponRequest() => new
    {
        Email = "invitee@example.com",
        AllowedTicketTypeIds = Array.Empty<Guid>(),
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
    };

    // Given the requester is a Crew member of the team
    // When they attempt to create a coupon for that team's event
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task CreateCoupon_CrewMember_Returns403()
    {
        var fixture = CouponAuthorizationFixture.BobIsCrewMember();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.CouponsRoute, NewCouponRequest(), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given the requester is an owner of a different team, not the target team
    // When they attempt to create a coupon for the target team's event
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task CreateCoupon_OwnerOfDifferentTeam_Returns403()
    {
        var fixture = CouponAuthorizationFixture.BobIsOwnerOfDifferentTeam();
        await fixture.SetupWithOtherTeamMembershipAsync(Environment);

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.CouponsRoute, NewCouponRequest(), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given an unauthenticated caller
    // When they attempt to create a coupon
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task CreateCoupon_Anonymous_Returns401()
    {
        var fixture = CouponAuthorizationFixture.NoTeamMembers();
        await fixture.SetupTeamOnlyAsync(Environment);

        var response = await Environment.AnonymousApiClient.PostAsJsonAsync(
            fixture.CouponsRoute, NewCouponRequest(), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a platform admin who is not a member of the team
    // When they create a coupon for that team's event
    // Then the API returns 201 Created
    [TestMethod]
    public async Task CreateCoupon_Admin_Returns201()
    {
        var fixture = CouponAuthorizationFixture.NoTeamMembers();
        await fixture.SetupTeamOnlyAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CouponsRoute,
            new
            {
                Email = "invitee@example.com",
                AllowedTicketTypeIds = new[] { fixture.TicketTypeId.Value },
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            },
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // Given the requester is a Crew member of the team
    // When they attempt to list that team's coupons
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task ListCoupons_CrewMember_Returns403()
    {
        var fixture = CouponAuthorizationFixture.BobIsCrewMember();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.CouponsRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given the requester is an owner of a different team, not the target team
    // When they attempt to list the target team's coupons
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task ListCoupons_OwnerOfDifferentTeam_Returns403()
    {
        var fixture = CouponAuthorizationFixture.BobIsOwnerOfDifferentTeam();
        await fixture.SetupWithOtherTeamMembershipAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.CouponsRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given an unauthenticated caller
    // When they attempt to list coupons
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task ListCoupons_Anonymous_Returns401()
    {
        var fixture = CouponAuthorizationFixture.NoTeamMembers();
        await fixture.SetupTeamOnlyAsync(Environment);

        var response = await Environment.AnonymousApiClient.GetAsync(
            fixture.CouponsRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a platform admin who is not a member of the team
    // When they list that team's coupons
    // Then the API returns 200 OK
    [TestMethod]
    public async Task ListCoupons_Admin_Returns200()
    {
        var fixture = CouponAuthorizationFixture.NoTeamMembers();
        await fixture.SetupTeamOnlyAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.CouponsRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Given the requester is a Crew member of the team
    // When they attempt to get a coupon's details
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task GetCoupon_CrewMember_Returns403()
    {
        var fixture = CouponAuthorizationFixture.BobIsCrewMember();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.CouponRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given the requester is an owner of a different team, not the target team
    // When they attempt to get a coupon's details from the target team
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task GetCoupon_OwnerOfDifferentTeam_Returns403()
    {
        var fixture = CouponAuthorizationFixture.BobIsOwnerOfDifferentTeam();
        await fixture.SetupWithOtherTeamMembershipAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.CouponRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given an unauthenticated caller
    // When they attempt to get a coupon's details
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task GetCoupon_Anonymous_Returns401()
    {
        var fixture = CouponAuthorizationFixture.NoTeamMembers();
        await fixture.SetupTeamOnlyAsync(Environment);

        var response = await Environment.AnonymousApiClient.GetAsync(
            fixture.CouponRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a platform admin who is not a member of the team
    // When they get a coupon's details from that team's event
    // Then the API returns 200 OK
    [TestMethod]
    public async Task GetCoupon_Admin_Returns200()
    {
        var fixture = CouponAuthorizationFixture.NoTeamMembers();
        await fixture.SetupTeamOnlyAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.CouponRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
