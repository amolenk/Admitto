using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Badges.BadgeTypeManagement;

[TestClass]
public sealed class DeleteBadgeTypeTests(TestContext testContext) : EndToEndTestBase
{
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
