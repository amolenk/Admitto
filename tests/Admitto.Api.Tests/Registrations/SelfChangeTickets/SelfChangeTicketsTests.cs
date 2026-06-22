using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfChangeTickets;

[TestClass]
public sealed class SelfChangeTicketsTests(TestContext testContext) : EndToEndTestBase
{
    // Successful self-service ticket change returns 200
    [TestMethod]
    public async Task SelfChangeTickets_ValidChange_Returns200()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, workshopCapacity: 20, workshopUsed: 5);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeIds = new[] { SelfChangeTicketsFixture.WorkshopId.Value } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Registration not found returns 404
    [TestMethod]
    public async Task SelfChangeTickets_NotFound_Returns404()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        var unknownRoute = $"/api/events/{fixture.EventId.Value}/registrations/{Guid.NewGuid()}/tickets";

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, unknownRoute)
        {
            Content = JsonContent.Create(new { TicketTypeIds = new[] { SelfChangeTicketsFixture.WorkshopId.Value } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Workshop at capacity returns 400
    [TestMethod]
    public async Task SelfChangeTickets_CapacityFull_Returns400()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, workshopCapacity: 20, workshopUsed: 20);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeIds = new[] { SelfChangeTicketsFixture.WorkshopId.Value } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Registration window closed returns 400
    [TestMethod]
    public async Task SelfChangeTickets_RegistrationWindowClosed_Returns400()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, registrationWindowClosed: true);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeIds = new[] { SelfChangeTicketsFixture.WorkshopId.Value } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Cancelled registration returns 409
    [TestMethod]
    public async Task SelfChangeTickets_AlreadyCancelled_Returns409()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, alreadyCancelled: true);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeIds = new[] { SelfChangeTicketsFixture.WorkshopId.Value } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Unknown ticket type slug returns 400
    [TestMethod]
    public async Task SelfChangeTickets_UnknownTicketType_Returns400()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeIds = new[] { Guid.NewGuid() } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Identical ticket set is a no-op success returning 200
    [TestMethod]
    public async Task SelfChangeTickets_SameSelection_Returns200()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new { TicketTypeIds = new[] { SelfChangeTicketsFixture.GeneralAdmissionId.Value } })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task SelfChangeTickets_EmptyWaitlistCouponCode_Returns400ValidationProblem()
    {
        var fixture = SelfChangeTicketsFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePublicApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.ChangeTicketsRoute)
        {
            Content = JsonContent.Create(new
            {
                TicketTypeIds = new[] { SelfChangeTicketsFixture.WorkshopId.Value },
                WaitlistCouponCode = Guid.Empty
            })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
