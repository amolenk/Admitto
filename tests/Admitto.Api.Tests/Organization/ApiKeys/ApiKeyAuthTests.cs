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
    [TestMethod]
    public async Task PartnerEndpoint_NoApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        // Use a bare HttpClient (no X-Api-Key header)
        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.PostAsJsonAsync(
            $"/api/events/{fixture.EventId}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Bogus/unknown API key returns 401
    [TestMethod]
    public async Task PartnerEndpoint_BogusApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient("bogus-key-that-does-not-exist");
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.EventId}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Revoked API key returns 401
    [TestMethod]
    public async Task PartnerEndpoint_RevokedApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndRevokedApiKey();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.EventId}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // API key for Team A used against Team B's event returns normal not-found behavior
    [TestMethod]
    public async Task PartnerEndpoint_ApiKeyForOtherTeam_Returns404()
    {
        var fixture = ApiKeyAuthFixture.WithTwoTeamsAndEvents();
        await fixture.SetupAsync(Environment);

        // Use team-a's key against team-b's event.
        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.OtherEventId}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Valid API key for correct team returns 202
    [TestMethod]
    public async Task PartnerEndpoint_ValidApiKey_Returns202()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/events/{fixture.EventId}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

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

    [TestMethod]
    public async Task PartnerCouponDetails_NoApiKey_Returns401()
    {
        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.GetAsync(
            $"/api/events/{Guid.NewGuid()}/coupons/{Guid.NewGuid()}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
