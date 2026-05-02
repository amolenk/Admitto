using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

[TestClass]
public sealed class GetTicketedEventsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: List active events excludes archived — active and cancelled returned, archived excluded
    [TestMethod]
    public async ValueTask SC001_ListActiveEventsExcludesArchived_ActiveAndCancelledReturned_ArchivedExcluded()
    {
        // Arrange
        var fixture = GetTicketedEventsFixture.WithMixedStatuses();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketedEventsQuery(fixture.TeamId);
        var sut = new GetTicketedEventsHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(e => e.Slug == "conf-2026");
        result.ShouldContain(e => e.Slug == "meetup-q1");
        result.ShouldNotContain(e => e.Slug == "conf-2025");
    }
}
