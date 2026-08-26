using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Badges.BadgeTypes;

[TestClass]
public sealed class AddBadgeTypeTests(TestContext testContext) : EndToEndTestBase
{
    // Given an active event
    // When a standalone badge type is added
    // Then the API returns 201 Created with the new badge type id
    [TestMethod]
    public async Task AddBadgeType_StandaloneType_Returns201WithId()
    {
        var fixture = BadgesApiFixture.Active();
        await fixture.SetupAsync(Environment);

        var request = new { Name = "Speaker Badge", Kind = "Standalone" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeTypesRoute, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>(testContext.CancellationToken);
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    // Given an active event
    // When a ticket-based badge type is added with a ticket type id
    // Then the API returns 201 Created with the new badge type id
    [TestMethod]
    public async Task AddBadgeType_TicketBasedType_Returns201WithId()
    {
        var fixture = BadgesApiFixture.Active();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            Name = "GA Badge",
            Kind = "TicketBased",
            TicketTypeIds = new[] { BadgesApiFixture.TicketTypeAId }
        };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeTypesRoute, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>(testContext.CancellationToken);
        body!.Id.ShouldNotBe(Guid.Empty);
    }

    // Given an active event
    // When a ticket-based badge type is added with no ticket type ids
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task AddBadgeType_TicketBasedWithEmptyTicketTypeIds_Returns400()
    {
        // FluentValidation rejects ticket-based with no ticket type IDs.
        var fixture = BadgesApiFixture.Active();
        await fixture.SetupAsync(Environment);

        var request = new { Name = "GA Badge", Kind = "TicketBased", TicketTypeIds = Array.Empty<Guid>() };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeTypesRoute, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given an existing badge type with a given name
    // When a new badge type is added with the same name
    // Then the API returns 409 Conflict
    [TestMethod]
    public async Task AddBadgeType_DuplicateName_Returns409()
    {
        var fixture = BadgesApiFixture.Active();
        fixture.AddStandaloneBadgeType("Speaker Badge");
        await fixture.SetupAsync(Environment);

        var request = new { Name = "Speaker Badge", Kind = "Standalone" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeTypesRoute, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // Given an archived event
    // When a badge type is added
    // Then the API returns 400 Bad Request
    [TestMethod]
    public async Task AddBadgeType_ArchivedEvent_Returns400()
    {
        // EnsureEventActive raises a validation error when the BadgesEvent is archived.
        var fixture = BadgesApiFixture.Archived();
        await fixture.SetupAsync(Environment);

        var request = new { Name = "Speaker Badge", Kind = "Standalone" };

        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.BadgeTypesRoute, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Given a user who is only a crew member of the team
    // When they attempt to add a badge type
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task AddBadgeType_CrewMember_Returns403()
    {
        var fixture = BadgesApiFixture.Active();
        await fixture.SetupAsync(Environment);

        var request = new { Name = "Speaker Badge", Kind = "Standalone" };

        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.BadgeTypesRoute, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private sealed record IdResponse(Guid Id);
}
