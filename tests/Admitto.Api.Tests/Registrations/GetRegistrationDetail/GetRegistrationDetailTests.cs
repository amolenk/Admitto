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

    [TestMethod]
    public async Task NonMember_Returns403()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.RegistrationRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

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
