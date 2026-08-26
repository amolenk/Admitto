using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.ExportRegistrations;

[TestClass]
public sealed class ExportRegistrationsTests(TestContext testContext) : EndToEndTestBase
{
    // Given an event with multiple registrations
    // When the registrations are exported
    // Then the API returns a CSV file containing each attendee's data
    [TestMethod]
    public async Task ExportRegistrations_WithRegistrations_ReturnsCsvWithAttendeeData()
    {
        var fixture = ExportRegistrationsFixture.HappyFlow();
        fixture.AddRegistration("alice@example.com", "Alice", "Smith");
        fixture.AddRegistration("bob@example.com", "Bob", "Jones");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/csv");

        var filename = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
        filename.ShouldBe("registrations-dev-conf.csv");

        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].ShouldBe("FirstName,LastName,Email,Tickets,Status,RegisteredAt");
        lines.ShouldContain(l => l.Contains("Alice") && l.Contains("Smith") && l.Contains("alice@example.com"));
        lines.ShouldContain(l => l.Contains("Bob") && l.Contains("Jones") && l.Contains("bob@example.com"));
    }

    // Given an event with both an active and a cancelled registration
    // When the registrations are exported
    // Then the CSV includes the cancelled attendee with their cancelled status
    [TestMethod]
    public async Task ExportRegistrations_WithCancelledRegistration_IncludesCancelledWithStatus()
    {
        var fixture = ExportRegistrationsFixture.HappyFlow();
        fixture.AddRegistration("alice@example.com", "Alice", "Smith");
        fixture.AddRegistration("cancelled@example.com", "Cancelled", "Person", cancelled: true);
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        csv.ShouldContain("cancelled@example.com");
        csv.ShouldContain("Cancelled");
    }

    // Given an event configured with an additional registration detail field
    // When the registrations are exported
    // Then the CSV includes an extra column with that detail's value
    [TestMethod]
    public async Task ExportRegistrations_WithAdditionalDetailSchema_IncludesDetailColumns()
    {
        var fixture = ExportRegistrationsFixture.HappyFlow()
            .WithAdditionalDetailField("company", "Company");
        fixture.AddRegistration(
            "alice@example.com",
            "Alice",
            "Smith",
            additionalDetails: new Dictionary<string, string> { ["company"] = "Acme" });
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].ShouldBe("FirstName,LastName,Email,Tickets,Status,RegisteredAt,Company");
        lines.ShouldContain(l => l.Contains("Alice") && l.Contains("Acme"));
    }

    // Given an event with no registrations
    // When the registrations are exported
    // Then the CSV contains only the header row
    [TestMethod]
    public async Task ExportRegistrations_NoRegistrations_ReturnsHeaderOnly()
    {
        var fixture = ExportRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(1);
        lines[0].ShouldBe("FirstName,LastName,Email,Tickets,Status,RegisteredAt");
    }

    // Given no team exists for a given id
    // When registrations are exported for that unknown team
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task ExportRegistrations_UnknownTeam_Returns404()
    {
        var fixture = ExportRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{Guid.NewGuid()}/events/{Guid.NewGuid()}/registrations/export",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given a valid team but no event exists for a given id
    // When registrations are exported for that unknown event
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task ExportRegistrations_UnknownEvent_Returns404()
    {
        var fixture = ExportRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            $"/admin/teams/{fixture.TeamId}/events/{Guid.NewGuid()}/registrations/export",
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given a user who is not a member of the team owning the event
    // When that user requests the registrations export
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task ExportRegistrations_NonMember_Returns403()
    {
        var fixture = ExportRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
