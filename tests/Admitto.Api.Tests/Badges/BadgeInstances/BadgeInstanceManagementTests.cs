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

    // Given a standalone badge type
    // When a badge instance is added to it
    // Then the API returns 201 Created with the new instance id
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

    // Given a ticket-based badge type
    // When a badge instance is added to it
    // Then the API returns 400 Bad Request
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

    // Given a standalone badge type
    // When a badge instance is added with an empty display name
    // Then the API returns 400 Bad Request
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

    // Given an archived event with a standalone badge type
    // When a badge instance is added to it
    // Then the API returns 400 Bad Request
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

    // Given an existing badge instance
    // When it is updated with a valid request
    // Then the API returns 204 No Content
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

    // Given an existing badge instance
    // When it is updated with the matching expected version
    // Then the API returns 204 No Content
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

    // Given an existing badge instance
    // When it is updated with a stale expected version
    // Then the API returns 409 Conflict
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

    // Given a badge type with no matching badge instance
    // When an update is requested for a non-existent instance id
    // Then the API returns 404 Not Found
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

    // Given an existing badge instance
    // When it is deleted
    // Then the API returns 204 No Content
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

    // Given a badge type with no matching badge instance
    // When deletion is requested for a non-existent instance id
    // Then the API returns 404 Not Found
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

    // Given a standalone badge type with multiple badge instances
    // When the badge instances are listed
    // Then the API returns 200 OK with all the instances
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

    // Given a badge instance belonging to a different event but sharing the same badge type id
    // When the badge instances for the current event's badge type are listed
    // Then the instance from the other event is excluded from the results
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

    // Given a ticket-based badge type
    // When its badge instances are listed
    // Then the API returns 400 Bad Request
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
