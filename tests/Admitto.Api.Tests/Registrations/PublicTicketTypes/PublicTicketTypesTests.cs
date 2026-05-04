using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.PublicTicketTypes;

[TestClass]
public sealed class PublicTicketTypesTests(TestContext testContext) : EndToEndTestBase
{
    // SC001: Only active + self-service-enabled ticket types are returned
    [TestMethod]
    public async Task SC001_GetPublicTicketTypes_ReturnsOnlySelfServiceEnabledAndActiveTypes()
    {
        var fixture = PublicTicketTypesFixture.Create();
        await fixture.SetupAsync(Environment, catalog =>
        {
            catalog.AddTicketType(Slug.From("general"), DisplayName.From("General Admission"), [], 200, selfServiceEnabled: true);
            catalog.AddTicketType(Slug.From("vip"), DisplayName.From("VIP Pass"), [], 50, selfServiceEnabled: false);
            catalog.AddTicketType(Slug.From("early-bird"), DisplayName.From("Early Bird"), [], 100, selfServiceEnabled: true);
            catalog.CancelTicketType(Slug.From("early-bird")); // cancelled
        });

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        var items = body.EnumerateArray().ToList();
        items.Count.ShouldBe(1);
        items[0].GetProperty("slug").GetString().ShouldBe("general");
    }

    // SC002: Empty list returned when no self-service ticket types exist
    [TestMethod]
    public async Task SC002_GetPublicTicketTypes_NoSelfServiceTypes_ReturnsEmptyList()
    {
        var fixture = PublicTicketTypesFixture.Create();
        await fixture.SetupAsync(Environment, catalog =>
        {
            catalog.AddTicketType(Slug.From("vip"), DisplayName.From("VIP Pass"), [], 50, selfServiceEnabled: false);
        });

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        body.EnumerateArray().ToList().ShouldBeEmpty();
    }

    // SC003: 404 when event does not exist
    [TestMethod]
    public async Task SC003_GetPublicTicketTypes_NonExistentEvent_Returns404()
    {
        var fixture = PublicTicketTypesFixture.Create();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            $"/api/teams/{PublicTicketTypesFixture.TeamSlug}/events/nonexistent-event/ticket-types",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
