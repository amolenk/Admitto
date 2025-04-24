using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Tests.TicketedEvents;

[TestClass]
public class CreateTicketedEventTests : DistributedAppTestBase
{
    [TestMethod]
    public async Task CreateTicketedEvent()
    {
        // Arrange
        var nextYear = DateTime.Today.Year + 1;
        var request = new CreateTicketedEventRequest(
            "Test Event",
            new DateTimeOffset(nextYear, 1, 24, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(nextYear, 1, 25, 16, 0, 0, TimeSpan.Zero),
            DateTimeOffset.UtcNow,
            new DateTimeOffset(nextYear, 1, 23, 18, 0, 0, TimeSpan.Zero),
            [
                new TicketTypeDto("General Admission", "Default", 200),
            ]);
    
        // TODO Seed some data that can be re-used across tests
        var team = Team.Create("Acme");
        
        Context.Teams.Add(team);
        
        await Context.SaveChangesAsync();
        
        // // Act
        var response = await Api.PostAsJsonAsync($"/teams/{team.Id}/events/", request);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        
        var result = (await response.Content.ReadFromJsonAsync<CreateTicketedEventResponse>())!;
        
        result.ShouldSatisfyAllConditions(r => ((Guid?)r.Id).ShouldNotBeNull());
    }
}