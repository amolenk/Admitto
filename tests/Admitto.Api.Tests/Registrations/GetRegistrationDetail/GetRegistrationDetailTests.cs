using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetRegistrationDetail;

[TestClass]
public sealed class GetRegistrationDetailTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task SC001_Organizer_ReturnsFullRegistrationDetail()
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
        tickets[0].GetProperty("slug").GetString().ShouldBe(GetRegistrationDetailFixture.TicketTypeSlug);

        body.GetProperty("activities").GetArrayLength().ShouldBe(0);
    }

    [TestMethod]
    public async Task SC002_RegistrationNotFound_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            GetRegistrationDetailFixture.Route(),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SC003_UnknownTeamSlug_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/ghost-team/events/{GetRegistrationDetailFixture.EventSlug}/registrations/{fixture.RegistrationId.Value}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SC004_UnknownEventSlug_Returns404()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{GetRegistrationDetailFixture.TeamSlug}/events/ghost-event/registrations/{fixture.RegistrationId.Value}",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task SC005_NonMember_Returns403()
    {
        var fixture = GetRegistrationDetailFixture.WithActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(
            fixture.RegistrationRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
