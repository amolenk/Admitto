using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.ActivityLog;

/// <summary>
/// Verifies that the <c>DomainEventsInterceptor</c> correctly projects domain events into the
/// activity-log view table as part of the same database transaction — tested end-to-end through the
/// API so that the real DI pipeline and interceptor are in play.
/// </summary>
[TestClass]
public sealed class ActivityLogTests(TestContext testContext) : EndToEndTestBase
{
    // Registering an attendee via the API produces a single Registered activity entry.
    // Given a ticketed event with an available ticket type
    // When an attendee is registered through the admin API
    // Then the registration detail shows a single Registered activity entry
    [TestMethod]
    public async Task AdminRegisterAttendee_CreatesRegisteredActivityEntry()
    {
        var fixture = ActivityLogFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var registerResponse = await Environment.ApiClient.PostAsJsonAsync(
            fixture.RegisterRoute,
            new { FirstName = "Alice", LastName = "Doe", Email = "alice@example.com", TicketTypeIds = new[] { ActivityLogFixture.TicketTypeId.Value } },
            cancellationToken: testContext.CancellationToken);

        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registrationId = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken)).GetProperty("registrationId").GetGuid();

        var detailResponse = await Environment.ApiClient.GetAsync(
            fixture.RegistrationDetailRoute(registrationId),
            testContext.CancellationToken);

        detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await detailResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);

        var activities = body.GetProperty("activities").EnumerateArray().ToList();
        activities.Count.ShouldBe(1);
        activities[0].GetProperty("activityType").GetString().ShouldBe("Registered");
    }

    // Cancelling a registration via the API appends a Cancelled activity entry.
    // Given a registered attendee
    // When the registration is cancelled through the API
    // Then the registration detail shows both the Registered and Cancelled activity entries with the cancellation reason
    [TestMethod]
    public async Task CancelRegistration_AppendsCancelledActivityEntry()
    {
        var fixture = ActivityLogFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var registerResponse = await Environment.ApiClient.PostAsJsonAsync(
            fixture.RegisterRoute,
            new { FirstName = "Alice", LastName = "Doe", Email = "alice@example.com", TicketTypeIds = new[] { ActivityLogFixture.TicketTypeId.Value } },
            cancellationToken: testContext.CancellationToken);

        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registrationId = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken)).GetProperty("registrationId").GetGuid();

        var cancelResponse = await Environment.ApiClient.PostAsJsonAsync(
            fixture.CancelRoute(registrationId),
            new { Reason = "AttendeeRequest" },
            cancellationToken: testContext.CancellationToken);

        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var detailResponse = await Environment.ApiClient.GetAsync(
            fixture.RegistrationDetailRoute(registrationId),
            testContext.CancellationToken);

        detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await detailResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: testContext.CancellationToken);

        var activities = body.GetProperty("activities").EnumerateArray().ToList();
        activities.Count.ShouldBe(2);
        activities[0].GetProperty("activityType").GetString().ShouldBe("Registered");
        activities[1].GetProperty("activityType").GetString().ShouldBe("Cancelled");
        activities[1].GetProperty("metadata").GetString().ShouldBe("AttendeeRequest");
    }
}
