using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Organization.ApiKeyManagement;

[TestClass]
public sealed class ApiKeyAuthTests(TestContext testContext) : EndToEndTestBase
{
    // SC001: Create API key via admin endpoint returns 201 with raw key
    [TestMethod]
    public async Task SC001_CreateApiKey_ValidRequest_Returns201WithRawKey()
    {
        var fixture = ApiKeyAuthFixture.WithTeam();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            $"/admin/teams/{ApiKeyAuthFixture.TeamSlug}/api-keys",
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

    // SC002: Create API key without a name returns 422
    [TestMethod]
    public async Task SC002_CreateApiKey_MissingName_Returns400()
    {
        var fixture = ApiKeyAuthFixture.WithTeam();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            $"/admin/teams/{ApiKeyAuthFixture.TeamSlug}/api-keys",
            new { Name = "" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC003: Create API key with name too long returns 400
    [TestMethod]
    public async Task SC003_CreateApiKey_NameTooLong_Returns400()
    {
        var fixture = ApiKeyAuthFixture.WithTeam();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            $"/admin/teams/{ApiKeyAuthFixture.TeamSlug}/api-keys",
            new { Name = new string('x', 101) },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC004: List API keys returns 200 with array
    [TestMethod]
    public async Task SC004_GetApiKeys_ReturnsListWithSeededKey()
    {
        var fixture = ApiKeyAuthFixture.WithSeededApiKey();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{ApiKeyAuthFixture.TeamSlug}/api-keys",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetArrayLength().ShouldBeGreaterThan(0);
        var first = body.EnumerateArray().First();
        first.TryGetProperty("keyPrefix", out _).ShouldBeTrue();
        first.TryGetProperty("key", out _).ShouldBeFalse();
    }

    // SC005: Revoke API key returns 204
    [TestMethod]
    public async Task SC005_RevokeApiKey_ActiveKey_Returns204()
    {
        var fixture = ApiKeyAuthFixture.WithSeededApiKey();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"/admin/teams/{ApiKeyAuthFixture.TeamSlug}/api-keys/{fixture.ApiKeyId}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // SC006: Revoke already-revoked API key returns 409
    [TestMethod]
    public async Task SC006_RevokeApiKey_AlreadyRevoked_Returns409()
    {
        var fixture = ApiKeyAuthFixture.WithRevokedApiKey();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            $"/admin/teams/{ApiKeyAuthFixture.TeamSlug}/api-keys/{fixture.ApiKeyId}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // SC007: Revoke a key that belongs to a different team returns 404
    [TestMethod]
    public async Task SC007_RevokeApiKey_KeyFromDifferentTeam_Returns404()
    {
        var fixture = ApiKeyAuthFixture.WithTwoTeams();
        await fixture.SetupAsync(Environment);

        // Try to revoke team-b's key via team-a's route
        var response = await Environment.ApiClient.DeleteAsync(
            $"/admin/teams/{ApiKeyAuthFixture.TeamSlug}/api-keys/{fixture.OtherTeamApiKeyId}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // SC008: No X-Api-Key header returns 401
    [TestMethod]
    public async Task SC008_PublicEndpoint_NoApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        // Use a bare HttpClient (no X-Api-Key header)
        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.PostAsJsonAsync(
            $"/api/teams/{ApiKeyAuthFixture.TeamSlug}/events/{ApiKeyAuthFixture.EventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // SC009: Bogus/unknown API key returns 401
    [TestMethod]
    public async Task SC009_PublicEndpoint_BogusApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient("bogus-key-that-does-not-exist");
        var response = await client.PostAsJsonAsync(
            $"/api/teams/{ApiKeyAuthFixture.TeamSlug}/events/{ApiKeyAuthFixture.EventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // SC010: Revoked API key returns 401
    [TestMethod]
    public async Task SC010_PublicEndpoint_RevokedApiKey_Returns401()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndRevokedApiKey();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/teams/{ApiKeyAuthFixture.TeamSlug}/events/{ApiKeyAuthFixture.EventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // SC011: API key for Team A used against Team B's route returns 403
    [TestMethod]
    public async Task SC011_PublicEndpoint_ApiKeyForOtherTeam_Returns403()
    {
        var fixture = ApiKeyAuthFixture.WithTwoTeamsAndEvents();
        await fixture.SetupAsync(Environment);

        // Use team-a's key against team-b's event route
        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/teams/{ApiKeyAuthFixture.OtherTeamSlug}/events/{ApiKeyAuthFixture.OtherEventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // SC012: Valid API key for correct team returns 202
    [TestMethod]
    public async Task SC012_PublicEndpoint_ValidApiKey_Returns202()
    {
        var fixture = ApiKeyAuthFixture.WithTeamAndEvent();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            $"/api/teams/{ApiKeyAuthFixture.TeamSlug}/events/{ApiKeyAuthFixture.EventSlug}/otp/request",
            new { Email = "test@example.com" },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }
}
