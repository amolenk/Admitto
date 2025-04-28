using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

namespace Amolenk.Admitto.Application.Tests.Middleware;

[TestClass]
public class ValidationExceptionHandlerTests : DistributedAppTestBase
{
    [TestMethod]
    public async Task ValidationException_ReturnsProblemDetails()
    {
        // Arrange
        var request = CreateTicketedEventRequest(name: string.Empty); // Empty string to trigger validation exception

        // Act
        var response = await Api.PostAsJsonAsync($"/teams/{Guid.Empty}/events/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(nameof(request.Name));
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
        
        return new CreateTicketedEventRequest(name, startDateTime.Value, endDateTime.Value, 
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