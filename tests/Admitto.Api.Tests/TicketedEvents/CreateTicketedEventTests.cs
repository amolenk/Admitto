using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

namespace Amolenk.Admitto.Application.Tests.TicketedEvents;

[TestClass]
public class CreateTicketedEventTests : DistributedAppTestBase
{
    [TestMethod]
    public async Task CreateTicketedEvent_NameIsEmpty_ReturnsValidationProblem()
    {
        // Arrange
        var request = CreateRequest(name: string.Empty);

        // Act
        var response = await Api.PostAsJsonAsync($"/teams/{TestData.DefaultTeamId}/events/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(nameof(request.Name));
    }

    [TestMethod]
    public async Task CreateTicketedEvent_NameIsTooLong_ReturnsValidationProblem()
    {
        // Arrange
        var request = CreateRequest(name: "F".PadRight(101, 'o'));

        // Act
        var response = await Api.PostAsJsonAsync($"/teams/{TestData.DefaultTeamId}/events/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(nameof(request.Name));
    }
    
    [TestMethod]
    public async Task CreateTicketedEvent_StartDateTimeIsEmpty_ReturnsValidationProblem()
    {
        // Arrange
        var request = CreateRequest(startDateTime: DateTimeOffset.MinValue);

        // Act
        var response = await Api.PostAsJsonAsync($"/teams/{TestData.DefaultTeamId}/events/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(nameof(request.StartDateTime));
    }
    
    [TestMethod, DoNotParallelize]
    public async Task CreateTicketedEvent_ValidEvent_CreatesEvent()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var response = await Api.PostAsJsonAsync($"/teams/{TestData.DefaultTeamId}/events/", request);
               
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var result = (await response.Content.ReadFromJsonAsync<CreateTicketedEventResponse>())!;
        
        result.ShouldSatisfyAllConditions(r => ((Guid?)r.Id).ShouldNotBeNull());
    }
    
    [TestMethod, DoNotParallelize]
    public async Task CreateTicketedEvent_ValidEvent_AddsEventToTeam()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        await Api.PostAsJsonAsync($"/teams/{TestData.DefaultTeamId}/events/", request);
        
        // Assert
        var team = await Context.Teams.FindAsync(TestData.DefaultTeamId);
        
        team.ShouldNotBeNull().ActiveEvents.ShouldContain(e => e.Name == request.Name);
    }

    private static CreateTicketedEventRequest CreateRequest(string? name = null, DateTimeOffset? startDateTime = null,
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