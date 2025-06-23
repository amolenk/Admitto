using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;
using Amolenk.Admitto.IntegrationTests.TestHelpers;

namespace Amolenk.Admitto.IntegrationTests.Middleware;

[TestClass]
public class DomainExceptionHandlerTests : FullStackTestsBase
{
    [TestMethod]
    public async Task DomainException_ReturnsProblemDetails()
    {
        // Arrange
        var nextYear = DateTime.Today.Year + 1;
        var offset = TimeSpan.Zero;
        
        // Create a request with the registration period ending after the start date. This will trigger
        // a domain exception.
        var request = CreateTicketedEventRequest(
            startDateTime: new DateTimeOffset(nextYear, 1, 24, 9, 0, 0, offset),
            endDateTime: new DateTimeOffset(nextYear, 1, 24, 17, 0, 0, offset),
            registrationStartDateTime: DateTimeOffset.Now,
            registrationEndDateTime: new DateTimeOffset(nextYear, 1, 24, 18, 0, 0, offset));

        // Act
        var response = await ApiClient.PostAsJsonAsync($"/teams/{DefaultTeam.Id}/events/", request);
               
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Title.ShouldBe("A domain error occured."));
    }
    
    private static CreateTicketedEventRequest CreateTicketedEventRequest(string? name = null, DateTimeOffset? startDateTime = null,
        DateTimeOffset? endDateTime = null, DateTimeOffset? registrationStartDateTime = null,
        DateTimeOffset? registrationEndDateTime = null, IEnumerable<TicketTypeDto>? ticketTypes = null)
    {
        var nextYear = DateTime.Today.Year + 1;
        var offset = TimeSpan.Zero;
        
        name ??= "Test Event";
        startDateTime ??= new DateTimeOffset(nextYear, 1, 24, 9, 0, 0, offset);
        endDateTime ??= new DateTimeOffset(nextYear, 1, 25, 16, 0, 0, offset);
        registrationStartDateTime ??= DateTimeOffset.UtcNow;
        registrationEndDateTime ??= new DateTimeOffset(nextYear, 1, 23, 18, 0, 0, offset);
        ticketTypes ??= [CreateTicketTypeDto()];
        
        return new CreateTicketedEventRequest(name, Guid.Empty, startDateTime.Value, endDateTime.Value, 
            registrationStartDateTime.Value, registrationEndDateTime.Value, ticketTypes);
    }
    
    private static TicketTypeDto CreateTicketTypeDto(string? name = null, string? slotName = null, int? quantity = null)
    {
        name ??= "General Admission";
        slotName ??= "Default";
        quantity ??= 100;
        
        return new TicketTypeDto(name, slotName, quantity.Value);
    }
}