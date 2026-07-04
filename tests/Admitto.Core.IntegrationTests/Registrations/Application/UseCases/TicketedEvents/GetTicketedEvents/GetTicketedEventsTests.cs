using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEvents;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.GetTicketedEvents;

[TestClass]
public sealed class GetTicketedEventsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Events are ordered by start date ascending — soonest event first
    [TestMethod]
    public async ValueTask ListActiveEvents_MultipleEvents_ReturnedSoonestFirst()
    {
        // Arrange
        var fixture = GetTicketedEventsFixture.WithMixedStatuses();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketedEventsQuery(fixture.TeamId);
        var sut = new GetTicketedEventsHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert — "Meetup Q1" starts in ~10 days, "Conf 2026" starts in ~30 days
        result[0].Name.ShouldBe("Meetup Q1");
        result[1].Name.ShouldBe("Conf 2026");
    }

    // List active events excludes archived — only active events returned
    [TestMethod]
    public async ValueTask ListActiveEventsExcludesArchived_ActiveEventsReturned_ArchivedExcluded()
    {
        // Arrange
        var fixture = GetTicketedEventsFixture.WithMixedStatuses();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketedEventsQuery(fixture.TeamId);
        var sut = new GetTicketedEventsHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(e => e.Name == "Conf 2026");
        result.ShouldContain(e => e.Name == "Meetup Q1");
        result.ShouldNotContain(e => e.Name == "Conf 2025");
    }
}
