using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Badges.ExportBadgeCsv;

[TestClass]
public sealed class ExportBadgeCsvTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task ExportBadgeCsv_StandaloneTypeWithInstances_ReturnsCsvWithDisplayNameAndNotes()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        fixture.AddBadgeInstance(badgeTypeId, "Alice Smith", "Keynote");
        fixture.AddBadgeInstance(badgeTypeId, "Bob Jones", "");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.ExportRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/csv");

        var filename = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
        filename.ShouldBe("badges-speaker-badge.csv");

        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].ShouldBe("DisplayName,Notes");
        // Ordered by DisplayName ascending
        lines[1].ShouldContain("Alice Smith");
        lines[2].ShouldContain("Bob Jones");
    }

    [TestMethod]
    public async Task ExportBadgeCsv_StandaloneTypeEmpty_ReturnsHeaderOnly()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.ExportRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(1);
        lines[0].ShouldBe("DisplayName,Notes");
    }

    [TestMethod]
    public async Task ExportBadgeCsv_TicketBasedTypeWithRegistrations_ReturnsCsvWithAttendeeData()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddTicketBasedBadgeType("GA Badge", [BadgesApiFixture.TicketTypeAId]);
        fixture.AddRegistration("alice@example.com", "Alice", "Smith");
        fixture.AddRegistration("bob@example.com", "Bob", "Jones");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.ExportRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].ShouldBe("FirstName,LastName,Email");
        lines.ShouldContain(l => l.Contains("Alice") && l.Contains("Smith") && l.Contains("alice@example.com"));
        lines.ShouldContain(l => l.Contains("Bob") && l.Contains("Jones") && l.Contains("bob@example.com"));
    }

    [TestMethod]
    public async Task ExportBadgeCsv_TicketBasedTypeWithCancelledRegistration_CancelledNotIncluded()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddTicketBasedBadgeType("GA Badge", [BadgesApiFixture.TicketTypeAId]);
        fixture.AddRegistration("alice@example.com", "Alice", "Smith");
        fixture.AddRegistration("cancelled@example.com", "Cancelled", "Person", cancelled: true);
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.ExportRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        csv.ShouldNotContain("cancelled@example.com");
        csv.ShouldContain("alice@example.com");
    }

    [TestMethod]
    public async Task ExportBadgeCsv_TicketBasedTypeEmpty_ReturnsHeaderOnly()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddTicketBasedBadgeType("GA Badge");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.ExportRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var csv = await response.Content.ReadAsStringAsync(testContext.CancellationToken);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(1);
        lines[0].ShouldBe("FirstName,LastName,Email");
    }

    [TestMethod]
    public async Task ExportBadgeCsv_NotFound_Returns404()
    {
        var fixture = BadgesApiFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.ExportRoute(Guid.NewGuid()), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
