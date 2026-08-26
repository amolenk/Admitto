using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetRegistrationDetail;

[TestClass]
public sealed class GetRegistrationDetailTests(TestContext testContext) : EndToEndTestBase
{
    // Given an active registration
    // When an organizer requests the registration detail
    // Then the API returns the full registration detail including tickets and activities
    [TestMethod]
    public async Task Organizer_ReturnsFullRegistrationDetail()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.RegistrationRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("id").GetGuid().ShouldBe(fixture.RegistrationId.Value);
        body.GetProperty("email").GetString().ShouldBe("alice@example.com");
        body.GetProperty("firstName").GetString().ShouldBe("Alice");
        body.GetProperty("lastName").GetString().ShouldBe("Doe");
        body.GetProperty("status").GetString().ShouldBe("registered");
        body.GetProperty("hasReconfirmed").GetBoolean().ShouldBeFalse();

        var tickets = body.GetProperty("tickets").EnumerateArray().ToList();
        tickets.ShouldHaveSingleItem();
        tickets[0].GetProperty("id").GetString().ShouldBe(GetRegistrationDetailFixture.TicketTypeId.Value.ToString());

        body.GetProperty("activities").GetArrayLength().ShouldBe(0);
    }

    // Given no registration exists with the requested id
    // When the registration detail is requested
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task RegistrationNotFound_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{fixture.TeamId}/events/{fixture.EventId}/registrations/{Guid.NewGuid()}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given an active registration
    // When the registration detail is requested under an unknown team
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task UnknownTeamSlug_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{Guid.NewGuid()}/events/{fixture.EventId}/registrations/{fixture.RegistrationId.Value}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given an active registration
    // When the registration detail is requested under an unknown event
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task UnknownEventSlug_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{fixture.TeamId}/events/{Guid.NewGuid()}/registrations/{fixture.RegistrationId.Value}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given an active registration and a user who is not a member of the owning team
    // When that user requests the registration detail
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task NonMember_Returns403()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.RegistrationRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given a partner registration
    // When the partner requests the registration detail using an API key
    // Then the API returns a reduced registration detail without organizer-only fields
    [TestMethod]
    public async Task PartnerRegistrationDetail_ReturnsReducedRegistrationDetail()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.PartnerRegistrationRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("id").GetGuid().ShouldBe(fixture.RegistrationId.Value);
        body.GetProperty("email").GetString().ShouldBe("alice@example.com");
        body.GetProperty("firstName").GetString().ShouldBe("Alice");
        body.GetProperty("lastName").GetString().ShouldBe("Doe");
        body.GetProperty("status").GetString().ShouldBe("registered");
        body.GetProperty("additionalDetails").GetProperty("dietary").GetString().ShouldBe("vegan");

        var ticketTypeIds = body.GetProperty("ticketTypeIds").EnumerateArray().ToList();
        ticketTypeIds.ShouldHaveSingleItem();
        ticketTypeIds[0].GetString().ShouldBe(GetRegistrationDetailFixture.TicketTypeId.Value.ToString());

        var tickets = body.GetProperty("tickets").EnumerateArray().ToList();
        tickets.ShouldHaveSingleItem();
        tickets[0].GetProperty("id").GetString().ShouldBe(GetRegistrationDetailFixture.TicketTypeId.Value.ToString());
        tickets[0].GetProperty("name").GetString().ShouldBe("General Admission");

        body.TryGetProperty("registeredAt", out _).ShouldBeFalse();
        body.TryGetProperty("hasReconfirmed", out _).ShouldBeFalse();
        body.TryGetProperty("reconfirmedAt", out _).ShouldBeFalse();
        body.TryGetProperty("cancellationReason", out _).ShouldBeFalse();
        body.TryGetProperty("activities", out _).ShouldBeFalse();
    }

    // Given a partner registration and a verification token for the registrant's email
    // When the registration is resolved by that email with the verification token
    // Then the API returns 200 OK with the matching registration id
    [TestMethod]
    public async Task PartnerRegistrationResolve_VerifiedEmail_ReturnsRegistrationId()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);
        await fixture.SeedValidCodeAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var token = await VerifyOtpAsync(client, fixture, "alice@example.com");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            fixture.ResolvePartnerRegistrationRoute("alice@example.com"));
        request.Headers.Authorization = new("Bearer", token);

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        body.GetProperty("registrationId").GetGuid().ShouldBe(fixture.RegistrationId.Value);
    }

    // Given a partner registration
    // When the registration is resolved by email without a bearer verification token
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task PartnerRegistrationResolve_MissingVerificationToken_Returns401()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.ResolvePartnerRegistrationRoute("alice@example.com"),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a verification token issued for one email address
    // When the registration is resolved for a different email address using that token
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task PartnerRegistrationResolve_TokenEmailMismatch_Returns401()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);
        await fixture.SeedValidCodeAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var token = await VerifyOtpAsync(client, fixture, "alice@example.com");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            fixture.ResolvePartnerRegistrationRoute("mallory@example.com"));
        request.Headers.Authorization = new("Bearer", token);

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a verified email with no matching registration
    // When the registration is resolved by that email
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task PartnerRegistrationResolve_UnknownRegistrationEmail_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);
        await fixture.SeedValidCodeAsync(Environment, "nobody@example.com");

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var token = await VerifyOtpAsync(client, fixture, "nobody@example.com");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            fixture.ResolvePartnerRegistrationRoute("nobody@example.com"));
        request.Headers.Authorization = new("Bearer", token);

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given a partner registration
    // When the registration detail is requested with an API key but no authorization bearer token
    // Then the API returns 200 OK with the reduced registration detail
    [TestMethod]
    public async Task PartnerRegistrationDetail_WithoutAuthorizationBearerToken_ReturnsReducedRegistrationDetail()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.PartnerRegistrationRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async ValueTask<string> VerifyOtpAsync(
        HttpClient client,
        GetRegistrationDetailFixture fixture,
        string email)
    {
        var response = await client.PostAsJsonAsync(
            fixture.VerifyOtpRoute,
            new { Email = email, Code = GetRegistrationDetailFixture.KnownPlainCode },
            cancellationToken: testContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);
        return body.GetProperty("token").GetString()!;
    }

    // Given a partner registration
    // When the registration detail is requested without an API key
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task PartnerRegistrationDetail_MissingApiKey_Returns401()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);

        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.GetAsync(
            fixture.PartnerRegistrationRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a partner registration and an API key belonging to a different team
    // When the registration detail is requested with that API key
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task PartnerRegistrationDetail_ApiKeyForOtherTeam_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistrationAndOtherTeamApiKey();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.OtherTeamApiKey);
        var response = await client.GetAsync(
            fixture.PartnerRegistrationRoute,
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given a partner registration
    // When the detail of an unrelated, non-existent registration id is requested
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task PartnerRegistrationDetail_UnknownRegistration_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.PartnerRegistrationRouteFor(RegistrationId.New()),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given a registration that belongs to a different event
    // When its detail is requested through the current event's partner API
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task PartnerRegistrationDetail_RegistrationFromAnotherEvent_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithPartnerRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            fixture.PartnerRegistrationRouteFor(fixture.OtherEventRegistrationId),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
