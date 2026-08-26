using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.PartnerEventDetails;

[TestClass]
public sealed class PartnerEventDetailsTests(TestContext testContext) : EndToEndTestBase
{
    // Returns descriptive event metadata and additional detail fields in schema order.
    // Given an event with an allowed email domain and additional detail fields configured
    // When a partner fetches the event details
    // Then the API returns 200 OK with the event metadata and fields in schema order
    [TestMethod]
    public async Task GetPartnerEventDetails_ReturnsMetadataAndFields()
    {
        var fixture = PartnerEventDetailsFixture.Create(
            allowedEmailDomain: "@allowed.com",
            additionalDetailFields:
            [
                AdditionalDetailField.Create("company", "Company", 200),
                AdditionalDetailField.Create("dietary", "Dietary requirements", 500),
            ]);
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.EventDetailsRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);

        body.GetProperty("name").GetString().ShouldBe("DevConf");
        body.GetProperty("slug").GetString().ShouldBe(fixture.EventSlug);
        body.GetProperty("timeZone").GetString().ShouldBe("Europe/Amsterdam");
        body.GetProperty("isRegistrationOpen").GetBoolean().ShouldBeTrue();
        body.GetProperty("allowedEmailDomain").GetString().ShouldBe("@allowed.com");

        var fields = body.GetProperty("additionalDetailFields").EnumerateArray().ToList();
        fields.Count.ShouldBe(2);
        fields[0].GetProperty("key").GetString().ShouldBe("company");
        fields[0].GetProperty("name").GetString().ShouldBe("Company");
        fields[0].GetProperty("maxLength").GetInt32().ShouldBe(200);
        fields[1].GetProperty("key").GetString().ShouldBe("dietary");
    }

    // Does not leak internal admin-only fields.
    // Given an existing event
    // When a partner fetches the event details
    // Then the response does not include internal admin-only fields
    [TestMethod]
    public async Task GetPartnerEventDetails_DoesNotExposeInternalFields()
    {
        var fixture = PartnerEventDetailsFixture.Create();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.EventDetailsRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);

        body.TryGetProperty("id", out _).ShouldBeFalse();
        body.TryGetProperty("teamId", out _).ShouldBeFalse();
        body.TryGetProperty("version", out _).ShouldBeFalse();
        body.TryGetProperty("status", out _).ShouldBeFalse();
        body.TryGetProperty("reconfirmPolicy", out _).ShouldBeFalse();
        body.TryGetProperty("waitlistPolicy", out _).ShouldBeFalse();
    }

    // Empty field list when the event has no additional detail schema.
    // Given an event with no additional detail schema and no email domain restriction
    // When a partner fetches the event details
    // Then the response has an empty field list and a null allowed email domain
    [TestMethod]
    public async Task GetPartnerEventDetails_NoSchema_ReturnsEmptyFieldList()
    {
        var fixture = PartnerEventDetailsFixture.Create();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.EventDetailsRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);

        body.GetProperty("additionalDetailFields").EnumerateArray().ToList().ShouldBeEmpty();
        body.GetProperty("allowedEmailDomain").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // 404 when event slug does not exist for the team.
    // Given no event exists for a given slug
    // When a partner fetches event details for that unknown slug
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task GetPartnerEventDetails_NonExistentEvent_Returns404()
    {
        var fixture = PartnerEventDetailsFixture.Create();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync("/api/events/unknown-event", testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // 401 when the API key is missing.
    // Given an existing event
    // When event details are requested without an API key
    // Then the API returns 401 Unauthorized
    [TestMethod]
    public async Task GetPartnerEventDetails_MissingApiKey_Returns401()
    {
        var fixture = PartnerEventDetailsFixture.Create();
        await fixture.SetupAsync(Environment);

        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.GetAsync(fixture.EventDetailsRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
