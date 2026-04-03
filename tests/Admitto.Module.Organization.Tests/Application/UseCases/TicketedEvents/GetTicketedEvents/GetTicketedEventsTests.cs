using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvents;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.GetTicketedEvents;

[TestClass]
public sealed class GetTicketedEventsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC005_GetTicketedEvents_ExcludesArchivedEvents()
    {
        // Arrange
        // SC-005: Given a team has an active event, a cancelled event, and an archived event,
        // when the list of events is requested, the archived event is excluded.
        var fixture = GetTicketedEventsFixture.TeamWithActiveAndCancelledAndArchivedEvents();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketedEventsQuery(fixture.TeamId);
        var sut = new GetTicketedEventsHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);

        result.ShouldContain(e => e.Slug == fixture.ActiveEventSlug);
        result.ShouldContain(e => e.Slug == fixture.CancelledEventSlug);
        result.ShouldNotContain(e => e.Slug == fixture.ArchivedEventSlug);

        var activeItem = result.Single(e => e.Slug == fixture.ActiveEventSlug);
        activeItem.Status.ShouldBe("Active");

        var cancelledItem = result.Single(e => e.Slug == fixture.CancelledEventSlug);
        cancelledItem.Status.ShouldBe("Cancelled");
    }
}
