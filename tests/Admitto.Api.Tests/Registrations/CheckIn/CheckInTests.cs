using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.CheckIn;

[TestClass]
public sealed class CheckInTests(TestContext testContext) : EndToEndTestBase
{
    // Given an active registration
    // When an administrator checks it in with its credential
    // Then the API returns the attendee and authoritative check-in timestamp
    [TestMethod]
    public async Task CheckIn_ActiveRegistration_ReturnsSuccessAttendee()
    {
        var fixture = CheckInFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("success");
        body.GetProperty("registrationId").GetGuid().ShouldBe(fixture.RegistrationId.Value);
        body.GetProperty("name").GetString().ShouldBe("Alice Attendee");
        body.GetProperty("ticketSelections").GetArrayLength().ShouldBe(1);
        body.GetProperty("checkedInAt").GetDateTimeOffset().ShouldNotBe(default);
    }

    // Given a registration that has already been checked in
    // When the same credential is submitted again
    // Then the API returns AlreadyCheckedIn with the original timestamp
    [TestMethod]
    public async Task CheckIn_DuplicateCredential_ReturnsAlreadyCheckedIn()
    {
        var fixture = CheckInFixture.Active();
        await fixture.SetupAsync(Environment);
        var request = new { credential = fixture.RegistrationId.Value.ToString() };

        var first = await Environment.ApiClient.PostAsJsonAsync(fixture.CheckInRoute, request, testContext.CancellationToken);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        var firstTimestamp = firstBody.GetProperty("checkedInAt").GetDateTimeOffset();
        var second = await Environment.ApiClient.PostAsJsonAsync(fixture.CheckInRoute, request, testContext.CancellationToken);

        var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        secondBody.GetProperty("outcome").GetString().ShouldBe("alreadyCheckedIn");
        secondBody.GetProperty("checkedInAt").GetDateTimeOffset()
            .ShouldBeInRange(firstTimestamp.AddMilliseconds(-1), firstTimestamp.AddMilliseconds(1));
    }

    // Given an eligible registration
    // When two check-in requests arrive concurrently
    // Then exactly one succeeds and the other reports AlreadyCheckedIn
    [TestMethod]
    public async Task CheckIn_ConcurrentRequests_ReturnOneSuccessAndOneDuplicate()
    {
        var fixture = CheckInFixture.Active();
        await fixture.SetupAsync(Environment);
        var request = new { credential = fixture.RegistrationId.Value.ToString() };

        var responses = await Task.WhenAll(
            Environment.ApiClient.PostAsJsonAsync(fixture.CheckInRoute, request, testContext.CancellationToken),
            Environment.ApiClient.PostAsJsonAsync(fixture.CheckInRoute, request, testContext.CancellationToken));
        var bodies = await Task.WhenAll(
            responses[0].Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken),
            responses[1].Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken));
        var outcomes = bodies.Select(body => body.GetProperty("outcome").GetString()).ToList();

        outcomes.Count(outcome => outcome == "success").ShouldBe(1);
        outcomes.Count(outcome => outcome == "alreadyCheckedIn").ShouldBe(1);
    }

    // Given a cancelled registration
    // When its credential is submitted for check-in
    // Then the API returns Cancelled with scoped attendee data
    [TestMethod]
    public async Task CheckIn_CancelledRegistration_ReturnsCancelled()
    {
        var fixture = CheckInFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.CancelledRegistrationId.Value.ToString() },
            testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("cancelled");
        body.GetProperty("registrationId").GetGuid().ShouldBe(fixture.CancelledRegistrationId.Value);
    }

    // Given an archived event
    // When a credential is submitted for check-in
    // Then the API returns EventNotActive without attendee data
    [TestMethod]
    public async Task CheckIn_ArchivedEvent_ReturnsEventNotActiveWithoutAttendeeData()
    {
        var fixture = CheckInFixture.Archived();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("eventNotActive");
        body.GetProperty("registrationId").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("name").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // Given a credential belonging to another event
    // When it is submitted for the selected event
    // Then the API returns the generic InvalidForEvent outcome without attendee data
    [TestMethod]
    public async Task CheckIn_WrongEventCredential_ReturnsGenericInvalidForEvent()
    {
        var fixture = CheckInFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.OtherEventRegistrationId.Value.ToString() },
            testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("invalidForEvent");
        body.GetProperty("registrationId").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("ticketSelections").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // Given a malformed credential
    // When it is submitted for check-in
    // Then the API returns the same generic invalid outcome
    [TestMethod]
    public async Task CheckIn_MalformedCredential_ReturnsGenericInvalidForEvent()
    {
        var fixture = CheckInFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = "not-a-registration-id" },
            testContext.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);
        body.GetProperty("outcome").GetString().ShouldBe("invalidForEvent");
        body.GetProperty("registrationId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // Given a checked-in registration
    // When its detail is requested after check-in
    // Then checkedInAt and the CheckedIn activity are present
    [TestMethod]
    public async Task CheckIn_ThenGetDetail_ReturnsTimestampAndTimelineEntry()
    {
        var fixture = CheckInFixture.Active();
        await fixture.SetupAsync(Environment);
        var checkIn = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);
        var checkInBody = await checkIn.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);

        var response = await Environment.ApiClient.GetAsync(fixture.DetailRoute, testContext.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);

        body.GetProperty("checkedInAt").GetDateTimeOffset()
            .ShouldBeInRange(
                checkInBody.GetProperty("checkedInAt").GetDateTimeOffset().AddMilliseconds(-1),
                checkInBody.GetProperty("checkedInAt").GetDateTimeOffset().AddMilliseconds(1));
        body.GetProperty("activities").EnumerateArray()
            .ShouldContain(a => a.GetProperty("activityType").GetString() == "CheckedIn");
    }

    // Given registered, checked-in, and cancelled registrations
    // When the check-in summary is requested
    // Then expected excludes cancelled registrations and checked-in counts only timestamps
    [TestMethod]
    public async Task GetSummary_MixedRegistrationStates_ReturnsExpectedAndCheckedInCounts()
    {
        var fixture = CheckInFixture.Summary();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(fixture.SummaryRoute, testContext.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(testContext.CancellationToken);

        body.GetProperty("expectedCount").GetInt32().ShouldBe(2);
        body.GetProperty("checkedInCount").GetInt32().ShouldBe(2);
    }

    // Given an active event and a crew member of its team
    // When the crew member checks in an attendee
    // Then the API permits the operation
    [TestMethod]
    public async Task CheckIn_CrewMember_IsAllowed()
    {
        var fixture = CheckInFixture.BobIsCrew();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Given an active event and an organizer of its team
    // When the organizer checks in an attendee
    // Then the API permits the operation
    [TestMethod]
    public async Task CheckIn_Organizer_IsAllowed()
    {
        var fixture = CheckInFixture.BobIsOrganizer();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Given an active event and an owner of its team
    // When the owner checks in an attendee
    // Then the API permits the operation
    [TestMethod]
    public async Task CheckIn_Owner_IsAllowed()
    {
        var fixture = CheckInFixture.BobIsOwner();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Given an active event and a user who is not a member of its team
    // When that user checks in an attendee
    // Then the API returns Forbidden
    [TestMethod]
    public async Task CheckIn_NonMember_ReturnsForbidden()
    {
        var fixture = CheckInFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.CheckInRoute,
            new { credential = fixture.RegistrationId.Value.ToString() },
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given an event with more than twenty matching registrations
    // When lookup is requested with an exact id and a partial name
    // Then exact lookup works and partial lookup is bounded to twenty results
    [TestMethod]
    public async Task Lookup_ExactAndPartialQuery_ReturnsScopedBoundedCandidates()
    {
        var fixture = CheckInFixture.LookupCandidates();
        await fixture.SetupAsync(Environment);

        var exact = await Environment.ApiClient.GetFromJsonAsync<JsonElement>(
            fixture.LookupRoute(fixture.RegistrationId.Value.ToString()), testContext.CancellationToken);
        var partial = await Environment.ApiClient.GetFromJsonAsync<JsonElement>(
            fixture.LookupRoute("  candidate  "), testContext.CancellationToken);

        exact.GetArrayLength().ShouldBe(1);
        exact[0].GetProperty("registrationId").GetGuid().ShouldBe(fixture.RegistrationId.Value);
        partial.GetArrayLength().ShouldBe(20);
        partial.EnumerateArray().ShouldNotContain(c => c.GetProperty("registrationId").GetGuid() == fixture.OtherEventRegistrationId.Value);
    }

    // Given an active event with registrations
    // When lookup is requested with a one-character partial query
    // Then the API returns an empty result
    [TestMethod]
    public async Task Lookup_OneCharacterPartialQuery_ReturnsEmpty()
    {
        var fixture = CheckInFixture.LookupCandidates();
        await fixture.SetupAsync(Environment);

        var body = await Environment.ApiClient.GetFromJsonAsync<JsonElement>(
            fixture.LookupRoute("a"), testContext.CancellationToken);

        body.GetArrayLength().ShouldBe(0);
    }
}
