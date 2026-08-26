using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Organization.ApiKeys;

[TestClass]
public sealed class ApiKeyAuthTests(TestContext testContext) : EndToEndTestBase
{
    // Create API key via admin endpoint returns 201 with raw key
    // Given an existing team
    // When an admin creates an API key with a valid name
    // Then the API returns 201 Created with the raw key and an 8-character key prefix
    [TestMethod]
    public async Task CreateApiKey_ValidRequest_Returns201WithRawKey()
    {
        var fixture = ApiKeyAuthFixture.WithTeam();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            $"/admin/teams/{fixture.TeamId}/api-keys",
            new { Name = "My Key" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.TryGetProperty("key", out var keyProp).ShouldBeTrue();
        keyProp.GetString().ShouldNotBeNullOrEmpty();
        body.TryGetProperty("keyPrefix", out var prefixProp).ShouldBeTrue();
        prefixProp.GetString()!.Length.ShouldBe(8);
    }

    // Create API key without a name returns 422
    // Given an existing team
    // When an admin creates an API key with an empty name
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task CreateApiKey_MissingName_Returns400()
    {
        var fixture = ApiKeyAuthFixture.WithTeam();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            $"/admin/teams/{fixture.TeamId}/api-keys",
            new { Name = "" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Create API key with name too long returns 400
    // Given an existing team
    // When an admin creates an API key with a name exceeding the maximum length
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task CreateApiKey_NameTooLong_Returns400()
    {
        var fixture = ApiKeyAuthFixture.WithTeam();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            $"/admin/teams/{fixture.TeamId}/api-keys",
            new { Name = new string('x', 101) },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // List API keys returns 200 with array
    // Given a team with a seeded API key
    // When an admin lists the team's API keys
    // Then the API returns 200 OK with the key's prefix but not its raw value
    [TestMethod]
    public async Task GetApiKeys_ReturnsListWithSeededKey()
    {
        var fixture = ApiKeyAuthFixture.WithSeededApiKey();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{fixture.TeamId}/api-keys",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetArrayLength().ShouldBeGreaterThan(0);
        var first = body.EnumerateArray().First();
        first.TryGetProperty("keyPrefix", out _).ShouldBeTrue();
        first.TryGetProperty("key", out _).ShouldBeFalse();
    }

    // Revoke API key returns 204
    // Given an active API key
    // When an admin revokes that key
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task RevokeApiKey_ActiveKey_Returns204()
    {
        var fixture = ApiKeyAuthFixture.WithSeededApiKey();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"/admin/teams/{fixture.TeamId}/api-keys/{fixture.ApiKeyId}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Revoke already-revoked API key returns 409
    // Given an API key that has already been revoked
    // When an admin attempts to revoke that same key again
    // Then the API returns 409 Conflict
    [TestMethod]
    public async Task RevokeApiKey_AlreadyRevoked_Returns409()
    {
        var fixture = ApiKeyAuthFixture.WithRevokedApiKey();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"/admin/teams/{fixture.TeamId}/api-keys/{fixture.ApiKeyId}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Revoke a key that belongs to a different team returns 404
    // Given two teams, each with their own API key
    // When an admin tries to revoke team B's key via team A's route
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task RevokeApiKey_KeyFromDifferentTeam_Returns404()
    {
        var fixture = ApiKeyAuthFixture.WithTwoTeams();
        await fixture.SetupAsync(Environment);

        // Try to revoke team-b's key via team-a's route
        var response = await Environment.ApiClient.DeleteAsync(
            $"/admin/teams/{fixture.TeamId}/api-keys/{fixture.OtherTeamApiKeyId}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // No X-Api-Key header returns 401
    // Given a partner endpoint for an existing event
    // When the request is sent without an X-Api-Key header
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task PartnerEndpoint_NoApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        // Use a bare HttpClient (no X-Api-Key header)
        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.PostAsJsonAsync(
            $"/api/events/{fixture.EventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Bogus/unknown API key returns 401
    // Given a partner endpoint for an existing event
    // When the request is sent with an API key that does not exist
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task PartnerEndpoint_BogusApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient("bogus-key-that-does-not-exist");
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.EventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Revoked API key returns 401
    // Given a team whose API key has been revoked
    // When a partner endpoint is called using that revoked key
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task PartnerEndpoint_RevokedApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndRevokedApiKey();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.EventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // API key for Team A used against Team B's event returns normal not-found behavior
    // Given two teams each with their own event and API key
    // When team A's API key is used to call a partner endpoint for team B's event
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task PartnerEndpoint_ApiKeyForOtherTeam_Returns404()
    {
        var fixture = ApiKeyAuthFixture.WithTwoTeamsAndEvents();
        await fixture.SetupAsync(Environment);

        // Use team-a's key against team-b's event.
        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.OtherEventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Valid API key for correct team returns 202
    // Given a team with a valid API key and an existing event
    // When a partner endpoint is called with that key for the team's own event
    // Then the API returns 202 Accepted
    [TestMethod]
    public async Task PartnerEndpoint_ValidApiKey_Returns202()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.EventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    // Given a valid API key and event
    // When the request uses the deprecated team-scoped route instead of the current one
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task PartnerEndpoint_OldTeamScopedRoute_Returns404()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/teams/{fixture.TeamId}/events/{fixture.EventId}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given a valid API key and event
    // When the request references the event by its id instead of its slug
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task PartnerEndpoint_EventIdRoute_Returns404()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.EventId}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given the coupon details partner endpoint for an unknown event
    // When the request is sent without an X-Api-Key header
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task PartnerCouponDetails_NoApiKey_Returns401()
    {
        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.GetAsync(
            $"/api/events/unknown-event/coupons/{Guid.NewGuid()}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
