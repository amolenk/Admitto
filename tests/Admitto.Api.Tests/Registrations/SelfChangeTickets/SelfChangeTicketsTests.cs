using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfChangeTickets;

[TestClass]
public sealed class SelfChangeTicketsTests(TestContext testContext) : EndToEndTestBase
{
    // SC001: Successful self-service ticket change returns 200
    [TestMethod]
    public async Task SC001_SelfChangeTickets_ValidChange_Returns200()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, workshopCapacity: 20, workshopUsed: 5);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeSlugs = new[] { "workshop" } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // SC002: Registration not found returns 404
    [TestMethod]
    public async Task SC002_SelfChangeTickets_NotFound_Returns404()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        var unknownRoute = $"/api/teams/{SelfChangeTicketsFixture.TeamSlug}/events/{SelfChangeTicketsFixture.EventSlug}/registrations/{Guid.NewGuid()}/tickets";

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, unknownRoute)
        {
            Content = JsonContent.Create(new { TicketTypeSlugs = new[] { "workshop" } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // SC003: Workshop at capacity returns 400
    [TestMethod]
    public async Task SC003_SelfChangeTickets_CapacityFull_Returns400()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, workshopCapacity: 20, workshopUsed: 20);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeSlugs = new[] { "workshop" } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC004: Registration window closed returns 400
    [TestMethod]
    public async Task SC004_SelfChangeTickets_RegistrationWindowClosed_Returns400()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, registrationWindowClosed: true);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeSlugs = new[] { "workshop" } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC005: Cancelled registration returns 409
    [TestMethod]
    public async Task SC005_SelfChangeTickets_AlreadyCancelled_Returns409()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, alreadyCancelled: true);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeSlugs = new[] { "workshop" } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // SC006: Unknown ticket type slug returns 400
    [TestMethod]
    public async Task SC006_SelfChangeTickets_UnknownTicketType_Returns400()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeSlugs = new[] { "nonexistent-ticket" } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC007: Identical ticket set is a no-op success returning 200
    [TestMethod]
    public async Task SC007_SelfChangeTickets_SameSelection_Returns200()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeSlugs = new[] { "general-admission" } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
