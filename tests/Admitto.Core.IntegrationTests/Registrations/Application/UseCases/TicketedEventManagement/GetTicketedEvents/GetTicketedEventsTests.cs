using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

[TestClass]
public sealed class GetTicketedEventsTests(TestContext testContext) : AspireIntegrationTestBase
{
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
