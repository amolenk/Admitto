using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.SharedScanner;

[TestClass]
public sealed class SharedScannerTests(TestContext testContext) : EndToEndTestBase
{
    // Given an active event with a valid shared scanner link
    // When the session bootstrap endpoint is called with its secret
    // Then it returns the event's team, id, and schedule without any sign-in
    [TestMethod]
    public async Task Session_ValidSecret_ReturnsEventContext()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.GetAsync(fixture.SessionRoute, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("teamId").GetGuid().ShouldBe(fixture.TeamId);
        body.GetProperty("eventId").GetGuid().ShouldBe(fixture.EventId);
    }

    // Given an active event with a valid shared scanner link
    // When an anonymous caller checks in an eligible registration via the link
    // Then the API returns success with the same attendee shape as the admin scanner
    [TestMethod]
    public async Task CheckIn_ValidLinkEligibleRegistration_ReturnsSuccess()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("success");
        body.GetProperty("registrationId").GetGuid().ShouldBe(fixture.RegistrationId.Value);
    }

    // Given a cancelled registration
    // When it is submitted through the shared scanner
    // Then the API returns Cancelled, preserving authoritative registration rules
    [TestMethod]
    public async Task CheckIn_CancelledRegistration_ReturnsCancelled()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.CancelledRegistrationId.Value.ToString() },
            testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("cancelled");
    }

    // Given a registration belonging to a different event
    // When it is submitted through this event's shared scanner link
    // Then the API returns the generic InvalidForEvent outcome
    [TestMethod]
    public async Task CheckIn_WrongEventCredential_ReturnsGenericInvalidForEvent()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.OtherEventRegistrationId.Value.ToString() },
            testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("invalidForEvent");
    }

    // Given a registration already checked in through the shared scanner
    // When the same credential is submitted again
    // Then the API returns AlreadyCheckedIn, preserving one-way attendance
    [TestMethod]
    public async Task CheckIn_DuplicateCredential_ReturnsAlreadyCheckedIn()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);
        var request = new { credential = fixture.RegistrationId.Value.ToString() };

        await Environment.AnonymousApiClient.PostAsJsonAsync(fixture.CheckInRoute, request, testContext.CancellationToken);
        var second = await Environment.AnonymousApiClient.PostAsJsonAsync(fixture.CheckInRoute, request, testContext.CancellationToken);

        var body = await second.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("alreadyCheckedIn");
    }

    // Given an eligible registration reachable through a valid shared scanner link
    // When two check-in requests arrive concurrently
    // Then exactly one succeeds and the other reports AlreadyCheckedIn
    [TestMethod]
    public async Task CheckIn_ConcurrentRequests_ReturnOneSuccessAndOneDuplicate()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);
        var request = new { credential = fixture.RegistrationId.Value.ToString() };

        var responses = await Task.WhenAll(
            Environment.AnonymousApiClient.PostAsJsonAsync(fixture.CheckInRoute, request, testContext.CancellationToken),
            Environment.AnonymousApiClient.PostAsJsonAsync(fixture.CheckInRoute, request, testContext.CancellationToken));
        var bodies = await Task.WhenAll(
            responses[0].Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken),
            responses[1].Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken));
        var outcomes = bodies.Select(body => body.GetProperty("outcome").GetString()).ToList();

        outcomes.Count(outcome => outcome == "success").ShouldBe(1);
        outcomes.Count(outcome => outcome == "alreadyCheckedIn").ShouldBe(1);
    }

    // Given an event whose shared scanner link has passed the event's scheduled end
    // When the link's secret is used for session bootstrap or check-in
    // Then the API returns Unauthorized with a neutral message
    [TestMethod]
    public async Task Session_ExpiredLink_ReturnsUnauthorized()
    {
        var fixture = SharedScannerFixture.ExpiredLink();
        await fixture.SetupAsync(Environment);

        var session = await Environment.AnonymousApiClient.GetAsync(fixture.SessionRoute, testContext.CancellationToken);
        var checkIn = await Environment.AnonymousApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        session.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        checkIn.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a revoked shared scanner link
    // When its secret is used for session bootstrap or check-in
    // Then the API returns Unauthorized with a neutral message
    [TestMethod]
    public async Task Session_RevokedLink_ReturnsUnauthorized()
    {
        var fixture = SharedScannerFixture.RevokedLink();
        await fixture.SetupAsync(Environment);

        var session = await Environment.AnonymousApiClient.GetAsync(fixture.SessionRoute, testContext.CancellationToken);
        var checkIn = await Environment.AnonymousApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        session.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        checkIn.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given an archived event that still has a scanner-link row
    // When its secret is used for session bootstrap or check-in
    // Then the API returns Unauthorized because the event is no longer Active
    [TestMethod]
    public async Task Session_ArchivedEvent_ReturnsUnauthorized()
    {
        var fixture = SharedScannerFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var session = await Environment.AnonymousApiClient.GetAsync(fixture.SessionRoute, testContext.CancellationToken);

        session.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given no event has ever created a shared scanner link
    // When a syntactically plausible but unknown secret is used
    // Then the API returns Unauthorized with the same neutral message
    [TestMethod]
    public async Task Session_MalformedOrUnknownSecret_ReturnsUnauthorized()
    {
        var fixture = SharedScannerFixture.NoLink();
        await fixture.SetupAsync(Environment);

        var unknown = await Environment.AnonymousApiClient.GetAsync(
            fixture.SessionRouteFor("not-a-real-secret"), testContext.CancellationToken);

        unknown.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given an active event with a valid shared scanner link
    // When an anonymous caller searches by name
    // Then the API returns matching eligible attendees scoped to that event
    [TestMethod]
    public async Task Lookup_ValidLinkMatchingName_ReturnsEligibleCandidateScopedToEvent()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.GetAsync(
            fixture.LookupRoute("Alice"), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetArrayLength().ShouldBe(1);
        var candidate = body[0];
        candidate.GetProperty("registrationId").GetGuid().ShouldBe(fixture.RegistrationId.Value);
        candidate.GetProperty("state").GetString().ShouldBe("eligible");
    }

    // Given a cancelled registration for the shared scanner's event
    // When an anonymous caller searches by name
    // Then the API returns it with a Cancelled state
    [TestMethod]
    public async Task Lookup_CancelledRegistration_ReturnsCancelledState()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.GetAsync(
            fixture.LookupRoute("Cancelled"), testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetArrayLength().ShouldBe(1);
        body[0].GetProperty("state").GetString().ShouldBe("cancelled");
    }

    // Given a registration already checked in through the shared scanner
    // When an anonymous caller searches by name
    // Then the API returns it with an already-checked-in state
    [TestMethod]
    public async Task Lookup_AlreadyCheckedInRegistration_ReturnsCheckedInState()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);
        await Environment.AnonymousApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        var response = await Environment.AnonymousApiClient.GetAsync(
            fixture.LookupRoute("Alice"), testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetArrayLength().ShouldBe(1);
        body[0].GetProperty("state").GetString().ShouldBe("checkedIn");
    }

    // Given a registration belonging to a different event
    // When an anonymous caller searches through this event's shared scanner link
    // Then the API does not return the other event's attendee
    [TestMethod]
    public async Task Lookup_OtherEventRegistration_IsNotReturned()
    {
        var fixture = SharedScannerFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.GetAsync(
            fixture.LookupRoute("Other"), testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetArrayLength().ShouldBe(0);
    }

    // Given an expired shared scanner link
    // When its secret is used for lookup
    // Then the API returns Unauthorized with a neutral message
    [TestMethod]
    public async Task Lookup_ExpiredLink_ReturnsUnauthorized()
    {
        var fixture = SharedScannerFixture.ExpiredLink();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.GetAsync(
            fixture.LookupRoute("Alice"), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Given a revoked shared scanner link
    // When its secret is used for lookup
    // Then the API returns Unauthorized with a neutral message
    [TestMethod]
    public async Task Lookup_RevokedLink_ReturnsUnauthorized()
    {
        var fixture = SharedScannerFixture.RevokedLink();
        await fixture.SetupAsync(Environment);

        var response = await Environment.AnonymousApiClient.GetAsync(
            fixture.LookupRoute("Alice"), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
