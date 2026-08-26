using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Badges.BadgeTypes;

[TestClass]
public sealed class RenameBadgeTypeTests(TestContext testContext) : EndToEndTestBase
{
    // Given an existing badge type
    // When it is renamed to a valid, unused name
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task RenameBadgeType_ValidName_Returns204()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Old Name");
        await fixture.SetupAsync(Environment);

        var request = new { Name = "New Name" };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Given an existing badge type
    // When it is renamed with the matching expected version
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task RenameBadgeType_WithCorrectVersion_Returns204()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Original");
        await fixture.SetupAsync(Environment);

        var request = new { Name = "Renamed", ExpectedVersion = fixture.BadgeTypeVersion(badgeTypeId) };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Given an existing badge type
    // When it is renamed with a stale expected version
    // Then the API returns 409 Conflict
    [TestMethod]
    public async Task RenameBadgeType_WithStaleVersion_Returns409()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Original");
        await fixture.SetupAsync(Environment);

        var staleVersion = fixture.BadgeTypeVersion(badgeTypeId) > 0 ? 0u : uint.MaxValue;
        var request = new { Name = "Renamed", ExpectedVersion = staleVersion };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Given two existing badge types with different names
    // When one is renamed to match the other's name
    // Then the API returns 409 Conflict
    [TestMethod]
    public async Task RenameBadgeType_DuplicateName_Returns409()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Badge A");
        fixture.AddStandaloneBadgeType("Badge B");
        await fixture.SetupAsync(Environment);

        // Rename "Badge A" to "Badge B" — conflicts with existing.
        var request = new { Name = "Badge B" };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Given a badge type belonging to an archived event
    // When it is renamed
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task RenameBadgeType_ArchivedEvent_Returns400()
    {
        var fixture = BadgesApiFixture.Archived();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Badge A");
        await fixture.SetupAsync(Environment);

        var request = new { Name = "Badge A Renamed" };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given no badge type exists with the given id
    // When a rename is requested for that id
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task RenameBadgeType_NotFound_Returns404()
    {
        var fixture = BadgesApiFixture.Active();
        await fixture.SetupAsync(Environment);

        var request = new { Name = "Renamed" };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeTypeRoute(Guid.NewGuid()), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
