using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Badges.BadgeTypes;

[TestClass]
public sealed class DeleteBadgeTypeTests(TestContext testContext) : EndToEndTestBase
{
    // Given an existing standalone badge type
    // When it is deleted
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task DeleteBadgeType_StandaloneType_Returns204()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Given an existing ticket-based badge type
    // When it is deleted
    // Then the API returns 204 No Content
    [TestMethod]
    public async Task DeleteBadgeType_TicketBasedType_Returns204()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddTicketBasedBadgeType("GA Badge");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Given a standalone badge type with a badge instance
    // When the badge type is deleted and its instances are then listed
    // Then listing returns 404 Not Found because the badge type no longer exists
    [TestMethod]
    public async Task DeleteBadgeType_CascadeDeletesInstances_ListInstancesReturns404AfterDeletion()
    {
        // After deleting a standalone badge type, listing its instances should return 404
        // (badge type no longer exists → NotFoundError from ListBadgeInstancesHandler).
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        fixture.AddBadgeInstance(badgeTypeId, "Alice");
        await fixture.SetupAsync(Environment);

        var deleteResponse = await Environment.ApiClient.DeleteAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), testContext.CancellationToken);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var listResponse = await Environment.ApiClient.GetAsync(
            fixture.BadgeInstancesRoute(badgeTypeId.Value), testContext.CancellationToken);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Given a badge type belonging to an archived event
    // When it is deleted
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task DeleteBadgeType_ArchivedEvent_Returns400()
    {
        var fixture = BadgesApiFixture.Archived();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            fixture.BadgeTypeRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given no badge type exists with the given id
    // When deletion is requested for that id
    // Then the API returns 404 Not Found
    [TestMethod]
    public async Task DeleteBadgeType_NotFound_Returns404()
    {
        var fixture = BadgesApiFixture.Active();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            fixture.BadgeTypeRoute(Guid.NewGuid()), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
