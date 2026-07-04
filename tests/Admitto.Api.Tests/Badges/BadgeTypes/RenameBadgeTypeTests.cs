using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Badges.BadgeTypes;

[TestClass]
public sealed class RenameBadgeTypeTests(TestContext testContext) : EndToEndTestBase
{
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
