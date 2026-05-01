using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.CancelRegistration;

[TestClass]
public sealed class CancelRegistrationTests(TestContext testContext) : EndToEndTestBase
{
    // SC-C01: Admin cancels active registration with AttendeeRequest — returns 204
    [TestMethod]
    public async Task SC001_CancelRegistration_AttendeeRequest_Returns204()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { Reason = "AttendeeRequest" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // SC-C02: Admin cancels active registration with VisaLetterDenied — returns 204
    [TestMethod]
    public async Task SC002_CancelRegistration_VisaLetterDenied_Returns204()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { Reason = "VisaLetterDenied" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // SC-C03: Admin supplies TicketTypesRemoved reason — returns 400 (invalid)
    [TestMethod]
    public async Task SC003_CancelRegistration_InvalidReason_Returns400()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { Reason = "TicketTypesRemoved" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // SC-C04: Crew member (Bob) cannot cancel — returns 403
    [TestMethod]
    public async Task SC004_CancelRegistration_CrewMember_Returns403()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var request = new { Reason = "AttendeeRequest" };

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // SC-C05: Cancel non-existent registration — returns 404
    [TestMethod]
    public async Task SC005_CancelRegistration_NotFound_Returns404()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var fakeRoute = $"/admin/teams/{CancelRegistrationFixture.TeamSlug}/events/{CancelRegistrationFixture.EventSlug}/registrations/{Guid.NewGuid()}/cancel";
        var request = new { Reason = "AttendeeRequest" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fakeRoute, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
