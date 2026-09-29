using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.PromoteWaitlistEntry;

[TestClass]
public sealed class PromoteWaitlistEntryTests(TestContext testContext) : EndToEndTestBase
{
    // Given a sold-out ticket type with the VIP second in its waitlist queue
    // When an organizer promotes the VIP's waitlist entry
    // Then the API returns 201 Created with the issued coupon's id and location
    [TestMethod]
    public async Task PromoteWaitlistEntry_ActiveEntry_Returns201WithCouponId()
    {
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsync(
            fixture.Route, content: null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PromoteWaitlistEntryResponse>(
            testContext.CancellationToken);
        body.ShouldNotBeNull();
        body.CouponId.ShouldNotBe(Guid.Empty);
        response.Headers.Location!.ToString()
            .ShouldBe($"/teams/{fixture.TeamId}/events/{fixture.EventId}/coupons/{body.CouponId}");
    }

    // Given the VIP's waitlist entry was already promoted
    // When an organizer promotes the same entry again
    // Then the API returns 409 Conflict
    [TestMethod]
    public async Task PromoteWaitlistEntry_EntryAlreadyPromoted_Returns409()
    {
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);
        (await Environment.ApiClient.PostAsync(fixture.Route, content: null, testContext.CancellationToken))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await Environment.ApiClient.PostAsync(
            fixture.Route, content: null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Given a waitlist without the requested entry
    // When an organizer promotes that entry
    // Then the API returns 409 Conflict
    [TestMethod]
    public async Task PromoteWaitlistEntry_UnknownEntry_Returns409()
    {
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsync(
            fixture.RouteFor(Guid.NewGuid()), content: null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Given a user with only crew-level team access
    // When that user attempts to promote a waitlist entry
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task PromoteWaitlistEntry_CrewMember_Returns403()
    {
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.PostAsync(
            fixture.Route, content: null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given an unauthenticated caller
    // When they attempt to promote a waitlist entry
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task PromoteWaitlistEntry_Anonymous_Returns401()
    {
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.PostAsync(
            fixture.Route, content: null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record PromoteWaitlistEntryResponse(Guid CouponId);
}
