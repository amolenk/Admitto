using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.PublicTicketTypes;

[TestClass]
public sealed class PublicTicketTypesTests(TestContext testContext) : EndToEndTestBase
{
    // Only active + self-service-enabled ticket types are returned
    [TestMethod]
    public async Task GetPublicTicketTypes_ReturnsOnlySelfServiceEnabledAndActiveTypes()
    {
        var fixture = PublicTicketTypesFixture.Create();
        var generalId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var vipId = TicketTypeId.From(new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        await fixture.SetupAsync(Environment, catalog =>
        {
            catalog.AddTicketType(generalId, TicketTypeName.From("General Admission"), [], 200, selfServiceEnabled: true);
            catalog.AddTicketType(vipId, TicketTypeName.From("VIP Pass"), [], 50, selfServiceEnabled: false);
        });

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        var items = body.EnumerateArray().ToList();
        items.Count.ShouldBe(1);
        items[0].GetProperty("name").GetString().ShouldBe("General Admission");
    }

    // Empty list returned when no self-service ticket types exist
    [TestMethod]
    public async Task GetPublicTicketTypes_NoSelfServiceTypes_ReturnsEmptyList()
    {
        var fixture = PublicTicketTypesFixture.Create();
        var vipId2 = TicketTypeId.From(new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        await fixture.SetupAsync(Environment, catalog =>
        {
            catalog.AddTicketType(vipId2, TicketTypeName.From("VIP Pass"), [], 50, selfServiceEnabled: false);
        });

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        body.EnumerateArray().ToList().ShouldBeEmpty();
    }

    // 404 when event does not exist
    [TestMethod]
    public async Task GetPublicTicketTypes_NonExistentEvent_Returns404()
    {
        var fixture = PublicTicketTypesFixture.Create();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            $"/api/teams/{fixture.TeamId.Value}/events/{Guid.NewGuid()}/ticket-types",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
