using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.CancelRegistration;

[TestClass]
public sealed class CancelRegistrationTests(TestContext testContext) : EndToEndTestBase
{
    // Given an active registration
    // When an admin cancels it with reason AttendeeRequest
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task CancelRegistration_AttendeeRequest_Returns204()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { Reason = "AttendeeRequest" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Given an active registration
    // When an admin cancels it with reason VisaLetterDenied
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task CancelRegistration_VisaLetterDenied_Returns204()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { Reason = "VisaLetterDenied" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Given an active registration
    // When an admin cancels it with an invalid reason not allowed for admin cancellation
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task CancelRegistration_InvalidReason_Returns400()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { Reason = "TicketTypesRemoved" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given an active registration and a user with only crew-level team access
    // When that user attempts to cancel the registration
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task CancelRegistration_CrewMember_Returns403()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { Reason = "AttendeeRequest" };

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given no registration exists for a given id
    // When an admin attempts to cancel that non-existent registration
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task CancelRegistration_NotFound_Returns404()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var fakeRoute = $"/admin/teams/{fixture.TeamId}/events/{fixture.EventId}/registrations/{Guid.NewGuid()}/cancel";
        var request = new { Reason = "AttendeeRequest" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fakeRoute, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
