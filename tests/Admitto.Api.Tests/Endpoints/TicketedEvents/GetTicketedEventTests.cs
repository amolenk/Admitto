using Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

namespace Amolenk.Admitto.Application.Tests.TicketedEvents;

[TestClass]
public class GetTicketedEventsTests : DistributedAppTestBase
{
    [TestMethod, DoNotParallelize]
    public async Task GetTicketedEvent_EventExists_ReturnsEvent()
    {
        var ticketType = TestDataBuilder.CreateTicketType();
        var ticketedEvent = TestDataBuilder.CreateTicketedEvent(ticketTypes: [ticketType]);

        var team = (await Context.Teams.FindAsync(TestData.DefaultTeamId))!;
        team.AddActiveEvent(ticketedEvent);
        
        await SaveAndClearChangeTrackerAsync();
        
        // Act
        var response = await Api.GetAsync($"/teams/{TestData.DefaultTeamId}/events/{ticketedEvent.Id}");
    
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    
        var result = (await response.Content.ReadFromJsonAsync<GetTicketedEventResponse>())!;
    
        result.ShouldSatisfyAllConditions(
            r => r.Name.ShouldBe(ticketedEvent.Name),
            r => r.StartDateTime.ShouldBe(ticketedEvent.StartDateTime),
            r => r.EndDateTime.ShouldBe(ticketedEvent.EndDateTime),
            r => r.RegistrationStartDateTime.ShouldBe(ticketedEvent.RegistrationStartDateTime),
            r => r.RegistrationEndDateTime.ShouldBe(ticketedEvent.RegistrationEndDateTime),
            r => r.TicketTypes.Count().ShouldBe(1),
            r => r.TicketTypes.First().ShouldSatisfyAllConditions(
                tt => tt.Name.ShouldBe(ticketType.Name),
                tt => tt.SlotName.ShouldBe(ticketType.SlotName),
                tt => tt.MaxCapacity.ShouldBe(ticketType.MaxCapacity),
                tt => tt.RemainingCapacity.ShouldBe(ticketType.MaxCapacity)));
    }

    [TestMethod, DoNotParallelize]
    public async Task GetTicketedEvent_EventDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var unknownEventId = Guid.NewGuid();
        
        // Act
        var response = await Api.GetAsync($"/teams/{TestData.DefaultTeamId}/events/{unknownEventId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}