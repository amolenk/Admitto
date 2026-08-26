using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SelfUpdateRegistration;

[TestClass]
public sealed class SelfUpdateRegistrationTests(TestContext testContext) : EndToEndTestBase
{
    // Given an open registration with available workshop capacity
    // When the attendee submits a valid self-service update
    // Then the API returns 200 OK and the changes are persisted
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

    // Given an open registration
    // When the attendee submits an update missing the required first name
    // Then the API returns 400 Bad Request with a validation problem
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

    // Given an open registration
    // When the update request is sent without an API key
    // Then the API returns 401 Unauthorized
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

    // Given no registration exists for a given id
    // When an update is submitted for that non-existent registration
    // Then the API returns 404 Not Found
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

    // Given a registration belonging to one team
    // When the update is authenticated with an API key belonging to a different team
    // Then the API returns 404 Not Found
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

    // Given a registration that has already been cancelled
    // When the attendee attempts to submit a self-service update
    // Then the API returns 409 Conflict
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

    // Given an open registration
    // When an update is sent to the deprecated tickets route
    // Then the API returns 404 Not Found
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
