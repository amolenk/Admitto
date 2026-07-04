using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfUpdateRegistration;

[TestClass]
public sealed class SelfUpdateRegistrationTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task SelfUpdateRegistration_ValidUpdate_Returns200AndPersistsChanges()
    {
        var fixture = SelfUpdateRegistrationFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, workshopCapacity: 20, workshopUsed: 5);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.UpdateRoute)
        {
            Content = JsonContent.Create(ValidWorkshopRequest())
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await Environment.RegistrationsDatabase.WithContextAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.LastName.Value.ShouldBe("Anderson");
            registration.AdditionalDetails["dietary"].ShouldBe("vegan");
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(SelfUpdateRegistrationFixture.WorkshopId);
        });
    }

    [TestMethod]
    public async Task SelfUpdateRegistration_MissingFirstName_Returns400ValidationProblem()
    {
        var fixture = SelfUpdateRegistrationFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.UpdateRoute)
        {
            Content = JsonContent.Create(new
            {
                LastName = "Anderson",
                TicketTypeIds = new[] { SelfUpdateRegistrationFixture.WorkshopId.Value },
                AdditionalDetails = new Dictionary<string, string> { ["dietary"] = "vegan" }
            })
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SelfUpdateRegistration_MissingApiKey_Returns401()
    {
        var fixture = SelfUpdateRegistrationFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.UpdateRoute,
            ValidWorkshopRequest(),
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task SelfUpdateRegistration_RegistrationNotFound_Returns404()
    {
        var fixture = SelfUpdateRegistrationFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/events/{fixture.EventSlug}/registrations/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(ValidWorkshopRequest())
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SelfUpdateRegistration_ApiKeyForOtherTeam_Returns404()
    {
        var fixture = SelfUpdateRegistrationFixture.WithOtherTeamApiKey();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.OtherTeamApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.UpdateRoute)
        {
            Content = JsonContent.Create(ValidWorkshopRequest())
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SelfUpdateRegistration_CancelledRegistration_Returns409()
    {
        var fixture = SelfUpdateRegistrationFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment, alreadyCancelled: true);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.UpdateRoute)
        {
            Content = JsonContent.Create(ValidWorkshopRequest())
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task SelfUpdateRegistration_OldTicketsRoute_Returns404()
    {
        var fixture = SelfUpdateRegistrationFixture.WithOpenRegistration();
        await fixture.SetupAsync(Environment);

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Put, fixture.OldTicketsRoute)
        {
            Content = JsonContent.Create(ValidWorkshopRequest())
        };

        var response = await client.SendAsync(request, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static object ValidWorkshopRequest() => new
    {
        FirstName = "Alice",
        LastName = "Anderson",
        TicketTypeIds = new[] { SelfUpdateRegistrationFixture.WorkshopId.Value },
        AdditionalDetails = new Dictionary<string, string> { ["dietary"] = "vegan" }
    };
}
