using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.AdminReconfirmRegistration;

[TestClass]
public sealed class AdminReconfirmRegistrationTests(TestContext testContext) : EndToEndTestBase
{
    // Given an active, unreconfirmed registration
    // When an admin reconfirms it
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task ReconfirmRegistration_ActiveRegistration_Returns204()
    {
        var fixture = AdminReconfirmRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsync(
            fixture.Route, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Given an already reconfirmed registration
    // When an admin reconfirms it again
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task ReconfirmRegistration_CalledTwice_IsIdempotent()
    {
        var fixture = AdminReconfirmRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var first = await Environment.ApiClient.PostAsync(
            fixture.Route, null, testContext.CancellationToken);
        var second = await Environment.ApiClient.PostAsync(
            fixture.Route, null, testContext.CancellationToken);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Given a cancelled registration
    // When an admin attempts to reconfirm it
    // Then the API returns 409 Conflict
    [TestMethod]
    public async Task ReconfirmRegistration_CancelledRegistration_Returns409()
    {
        var fixture = AdminReconfirmRegistrationFixture.WithCancelledRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsync(
            fixture.Route, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Given a registration for an event whose catalog has been archived
    // When an admin attempts to reconfirm it
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task ReconfirmRegistration_EventArchived_Returns400()
    {
        var fixture = AdminReconfirmRegistrationFixture.WithArchivedEvent();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsync(
            fixture.Route, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given an active registration and a user with only crew-level team access
    // When that user attempts to reconfirm the registration
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task ReconfirmRegistration_CrewMember_Returns403()
    {
        var fixture = AdminReconfirmRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.PostAsync(
            fixture.Route, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given no registration exists for a given id
    // When an admin attempts to reconfirm that non-existent registration
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task ReconfirmRegistration_NotFound_Returns404()
    {
        var fixture = AdminReconfirmRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var fakeRoute =
            $"/admin/teams/{fixture.TeamId}/events/{fixture.EventId}/registrations/{Guid.NewGuid()}/reconfirm";

        var response = await Environment.ApiClient.PostAsync(
            fakeRoute, null, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
