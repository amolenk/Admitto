using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.GetTicketedEvents;

[TestClass]
public sealed class GetTicketedEventsTests(TestContext testContext) : EndToEndTestBase
{
    // SC002: Admin listing excludes archived events — active and cancelled returned, archived excluded
    [TestMethod]
    public async Task SC002_AdminListingExcludesArchivedEvents_ActiveAndCancelledReturned_ArchivedExcluded()
    {
        // Arrange
        var fixture = GetTicketedEventsFixture.WithMixedStatuses();
        await fixture.SetupAsync(Environment);

        // Act
        var response = await Environment.ApiClient.GetAsync(
            fixture.Route,
            testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var events = await response.Content.ReadFromJsonAsync<EventListItemDto[]>(
            cancellationToken: testContext.CancellationToken);
        events.ShouldNotBeNull();
        events.Length.ShouldBe(2);
        events.ShouldContain(e => e.Name == "Conf 2026");
        events.ShouldContain(e => e.Name == "Meetup Q1");
        events.ShouldNotContain(e => e.Name == "Conf 2025");
    }

    private sealed record EventListItemDto(
        Guid Id,
        string Name,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        string TimeZone,
        string Status);
}
