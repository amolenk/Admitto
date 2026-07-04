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
    // Only self-service-enabled ticket types are returned.
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

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        var items = body.EnumerateArray().ToList();
        items.Count.ShouldBe(1);
        items[0].GetProperty("name").GetString().ShouldBe("General Admission");
        items[0].GetProperty("status").GetString().ShouldBe("available");
    }

    [TestMethod]
    public async Task GetPublicTicketTypes_UnboundedTicket_ReturnsAvailableStatus()
    {
        var fixture = PublicTicketTypesFixture.Create();
        var generalId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        await fixture.SetupAsync(Environment, catalog =>
        {
            catalog.AddTicketType(generalId, TicketTypeName.From("General Admission"), [], null, selfServiceEnabled: true);
            catalog.GetTicketType(generalId)!.ClaimUncapped();
        });

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        var item = body.EnumerateArray().Single();
        item.GetProperty("status").GetString().ShouldBe("available");
    }

    [TestMethod]
    public async Task GetPublicTicketTypes_SoldOutWaitlistableTicket_ReturnsWaitlistStatus()
    {
        var fixture = PublicTicketTypesFixture.Create();
        var generalId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        await fixture.SetupAsync(Environment, catalog =>
        {
            catalog.AddTicketType(generalId, TicketTypeName.From("General Admission"), [], 1, selfServiceEnabled: true, waitlistEnabled: true);
            catalog.Claim([generalId], enforce: true);
        });

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        var item = body.EnumerateArray().Single();
        item.GetProperty("status").GetString().ShouldBe("waitlist");
    }

    [TestMethod]
    public async Task GetPublicTicketTypes_SoldOutNonWaitlistTicket_ReturnsSoldOutStatus()
    {
        var fixture = PublicTicketTypesFixture.Create();
        var generalId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        await fixture.SetupAsync(Environment, catalog =>
        {
            catalog.AddTicketType(generalId, TicketTypeName.From("General Admission"), [], 1, selfServiceEnabled: true);
            catalog.Claim([generalId], enforce: true);
        });

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        var item = body.EnumerateArray().Single();
        item.GetProperty("status").GetString().ShouldBe("soldOut");
    }

    [TestMethod]
    public async Task GetPublicTicketTypes_Response_DoesNotExposeInternalCapacityOrWaitlistFields()
    {
        var fixture = PublicTicketTypesFixture.Create();
        var generalId = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        await fixture.SetupAsync(Environment, catalog =>
        {
            catalog.AddTicketType(generalId, TicketTypeName.From("General Admission"), [], 200, selfServiceEnabled: true);
        });

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: testContext.CancellationToken);
        var item = body.EnumerateArray().Single();
        item.GetProperty("status").GetString().ShouldBe("available");
        item.TryGetProperty("soldOut", out _).ShouldBeFalse();
        item.TryGetProperty("requiresWaitlist", out _).ShouldBeFalse();
        item.TryGetProperty("maxCapacity", out _).ShouldBeFalse();
        item.TryGetProperty("usedCapacity", out _).ShouldBeFalse();
        item.TryGetProperty("waitlistEnabled", out _).ShouldBeFalse();
        item.TryGetProperty("waitlistMode", out _).ShouldBeFalse();
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

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
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

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.GetAsync(
            "/api/events/unknown-event/ticket-types",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetPublicTicketTypes_MissingApiKey_Returns401()
    {
        var fixture = PublicTicketTypesFixture.Create();
        await fixture.SetupAsync(Environment);

        using var bareClient = new HttpClient { BaseAddress = Environment.ApiClient.BaseAddress };
        var response = await bareClient.GetAsync(fixture.TicketTypesRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
