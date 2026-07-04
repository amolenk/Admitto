using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Badges.BadgeInstances;

[TestClass]
public sealed class BadgeInstanceManagementTests(TestContext testContext) : EndToEndTestBase
{
    // ─── Add ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task AddBadgeInstance_ToStandaloneType_Returns201WithId()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var request = new { DisplayName = "Alice Smith", Notes = "Keynote speaker" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeInstancesRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>(testContext.CancellationToken);
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    [TestMethod]
    public async Task AddBadgeInstance_ToTicketBasedType_Returns400()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddTicketBasedBadgeType("GA Badge");
        await fixture.SetupAsync(Environment);

        var request = new { DisplayName = "Alice Smith", Notes = "" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeInstancesRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task AddBadgeInstance_EmptyDisplayName_Returns400()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var request = new { DisplayName = "", Notes = "" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeInstancesRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task AddBadgeInstance_ArchivedEvent_Returns400()
    {
        var fixture = BadgesApiFixture.Archived();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var request = new { DisplayName = "Alice Smith", Notes = "" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeInstancesRoute(badgeTypeId.Value), request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ─── Update ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task UpdateBadgeInstance_ValidRequest_Returns204()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        var instanceId = fixture.AddBadgeInstance(badgeTypeId, "Alice Smith", "");
        await fixture.SetupAsync(Environment);

        var request = new { DisplayName = "Alice Smith (Updated)", Notes = "Workshop" };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeInstanceRoute(badgeTypeId.Value, instanceId.Value),
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task UpdateBadgeInstance_WithCorrectVersion_Returns204()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        var instanceId = fixture.AddBadgeInstance(badgeTypeId, "Alice Smith", "");
        await fixture.SetupAsync(Environment);

        var request = new { DisplayName = "Alice Smith (Updated)", Notes = "Workshop", ExpectedVersion = fixture.BadgeInstanceVersion(instanceId) };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeInstanceRoute(badgeTypeId.Value, instanceId.Value),
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task UpdateBadgeInstance_WithStaleVersion_Returns409()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        var instanceId = fixture.AddBadgeInstance(badgeTypeId, "Alice Smith", "");
        await fixture.SetupAsync(Environment);

        var staleVersion = fixture.BadgeInstanceVersion(instanceId) > 0 ? 0u : uint.MaxValue;
        var request = new { DisplayName = "Alice Smith (Updated)", Notes = "Workshop", ExpectedVersion = staleVersion };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeInstanceRoute(badgeTypeId.Value, instanceId.Value),
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task UpdateBadgeInstance_NotFound_Returns404()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var request = new { DisplayName = "Ghost", Notes = "" };

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.BadgeInstanceRoute(badgeTypeId.Value, Guid.NewGuid()),
            request,
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── Delete ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task DeleteBadgeInstance_ExistingInstance_Returns204()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        var instanceId = fixture.AddBadgeInstance(badgeTypeId, "Alice Smith", "");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            fixture.BadgeInstanceRoute(badgeTypeId.Value, instanceId.Value),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task DeleteBadgeInstance_NotFound_Returns404()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.DeleteAsync(
            fixture.BadgeInstanceRoute(badgeTypeId.Value, Guid.NewGuid()),
            testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── List ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ListBadgeInstances_StandaloneType_Returns200WithInstances()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        fixture.AddBadgeInstance(badgeTypeId, "Alice Smith", "Keynote");
        fixture.AddBadgeInstance(badgeTypeId, "Bob Jones", "Workshop");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.BadgeInstancesRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<BadgeInstanceDto>>(testContext.CancellationToken);
        items!.Count.ShouldBe(2);
        items.ShouldContain(i => i.DisplayName == "Alice Smith" && i.Notes == "Keynote");
        items.ShouldContain(i => i.DisplayName == "Bob Jones" && i.Notes == "Workshop");
    }

    [TestMethod]
    public async Task ListBadgeInstances_OtherEventInstanceWithSameBadgeTypeId_ExcludesOtherEventInstance()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddStandaloneBadgeType("Speaker Badge");
        fixture.AddBadgeInstance(badgeTypeId, "Alice Smith", "Keynote");
        await fixture.SetupAsync(Environment);

        await Environment.BadgesDatabase.SeedAsync(db => db.BadgeInstances.Add(
            BadgeInstance.Create(
                BadgeInstanceId.New(),
                TeamId.New(),
                TicketedEventId.New(),
                badgeTypeId,
                BadgeInstanceDisplayName.From("Mallory Jones"),
                BadgeInstanceNotes.From("Other event"))));

        var response = await Environment.ApiClient.GetAsync(
            fixture.BadgeInstancesRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<BadgeInstanceDto>>(testContext.CancellationToken);
        items!.Count.ShouldBe(1);
        items.ShouldContain(i => i.DisplayName == "Alice Smith");
        items.ShouldNotContain(i => i.DisplayName == "Mallory Jones");
    }

    [TestMethod]
    public async Task ListBadgeInstances_TicketBasedType_Returns400()
    {
        var fixture = BadgesApiFixture.Active();
        var badgeTypeId = fixture.AddTicketBasedBadgeType("GA Badge");
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.GetAsync(
            fixture.BadgeInstancesRoute(badgeTypeId.Value), testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record IdResponse(Guid Id);
    private sealed record BadgeInstanceDto(Guid Id, string DisplayName, string Notes);
}
