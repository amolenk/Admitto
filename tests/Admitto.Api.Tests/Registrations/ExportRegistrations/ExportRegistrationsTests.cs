using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.ExportRegistrations;

[TestClass]
public sealed class ExportRegistrationsTests(TestContext testContext) : EndToEndTestBase
{
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

    [TestMethod]
    public async Task ExportRegistrations_NonMember_Returns403()
    {
        var fixture = ExportRegistrationsFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var response = await Environment.BobApiClient.GetAsync(fixture.Route, testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
