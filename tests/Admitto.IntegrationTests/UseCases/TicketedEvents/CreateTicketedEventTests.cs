using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.IntegrationTests.UseCases.TicketedEvents;

[TestClass]
public class CreateTicketedEventTests : ApiTestsBase
{
    [DataTestMethod]
    [DataRow(null)]
    public async Task TeamIdIsInvalid_ReturnsBadRequest(Guid? value)
    {
        // Arrange
        var teamId = value is null ? null : new TeamId(value.Value); 
        
        var request = new CreateTicketedEventRequestBuilder()
            .WithTeamId(teamId)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey("teamId"));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("X")] // Name is too short
    [DataRow("012345678901234567890123456789012345678901234567891")] // Name is too long
    public async Task NameIsInvalid_ReturnsBadRequest(string? name)
    {
        // Arrange
        var request = new CreateTicketedEventRequestBuilder()
            .WithName(name!)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey("name"));
    }
    
    [DataTestMethod]
    [DataRow(null)]
    public async Task StartTimeIsInvalid_ReturnsBadRequest(DateTimeOffset? startTime)
    {
        // Arrange
        var request = new CreateTicketedEventRequestBuilder()
            .WithStartTime(startTime)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey("startTime"));
    }
    
    [DataTestMethod]
    [DataRow(null)]
    public async Task EndTimeIsInvalid_ReturnsBadRequest(DateTimeOffset? endTime)
    {
        // Arrange
        var request = new CreateTicketedEventRequestBuilder()
            .WithEndTime(endTime)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey("endTime"));
    }
    
    [DataTestMethod]
    [DataRow(null)]
    public async Task RegistrationStartTimeIsInvalid_ReturnsBadRequest(DateTimeOffset? startTime)
    {
        // Arrange
        var request = new CreateTicketedEventRequestBuilder()
            .WithRegistrationStartTime(startTime)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey("registrationStartTime"));
    }
    
    [DataTestMethod]
    [DataRow(null)]
    public async Task RegistrationEndTimeIsInvalid_ReturnsBadRequest(DateTimeOffset? endTime)
    {
        // Arrange
        var request = new CreateTicketedEventRequestBuilder()
            .WithRegistrationEndTime(endTime)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey("registrationEndTime"));
    }

    [DataTestMethod]
    // End time is before start time
    [DataRow("2024-01-24T09:00:00Z", "2024-01-23T16:00:00Z", "2023-09-01T08:00:00Z", "2024-01-22T18:00:00Z", "endTime")]
    // End time is the same as start time
    [DataRow("2024-01-24T09:00:00Z", "2024-01-24T09:00:00Z", "2023-09-01T08:00:00Z", "2024-01-22T18:00:00Z", "endTime")]
    // Registration start time is after start time
    [DataRow("2024-01-24T09:00:00Z", "2024-01-25T16:00:00Z", "2024-01-24T12:00:00Z", "2024-01-24T18:00:00Z", "registrationStartTime")]
    // Registration start time is the same as start time
    [DataRow("2024-01-24T09:00:00Z", "2024-01-25T16:00:00Z", "2024-01-24T09:00:00Z", "2024-01-24T18:00:00Z", "registrationStartTime")]
    // Registration start time is after registration end time
    [DataRow("2024-01-24T09:00:00Z", "2024-01-25T16:00:00Z", "2024-01-22T18:00:00Z", "2023-09-01T08:00:00Z", "registrationEndTime")]
    // Registration start time is the same as registration end time
    [DataRow("2024-01-24T09:00:00Z", "2024-01-25T16:00:00Z", "2024-01-22T18:00:00Z", "2024-01-22T18:00:00Z", "registrationEndTime")]
    // Registration end time is after start time
    [DataRow("2024-01-24T09:00:00Z", "2024-01-25T16:00:00Z", "2023-09-01T08:00:00Z", "2024-01-24T18:00:00Z", "registrationEndTime")]
    // Registration end time is the same as start time
    [DataRow("2024-01-24T09:00:00Z", "2024-01-25T16:00:00Z", "2023-09-01T08:00:00Z", "2024-01-24T09:00:00Z", "registrationEndTime")]
    public async Task TimesAreInvalid_ReturnsBadRequest(string startTimeValue, string endTimeValue, 
        string registrationStartTimeValue, string registrationEndTimeValue, string expectedErrorKey)
    {
        var startTime = DateTimeOffset.Parse(startTimeValue);
        var endTime = DateTimeOffset.Parse(endTimeValue);
        var registrationStartTime = DateTimeOffset.Parse(registrationStartTimeValue);
        var registrationEndTime = DateTimeOffset.Parse(registrationEndTimeValue);
        
        // Arrange
        var request = new CreateTicketedEventRequestBuilder()
            .WithStartTime(startTime)
            .WithEndTime(endTime)
            .WithRegistrationStartTime(registrationStartTime)
            .WithRegistrationEndTime(registrationEndTime)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey(expectedErrorKey));
    }
    
    
    
    [DataTestMethod]
    // Name is null
    [DataRow(null, "Default", 100, "name")]
    // Name is empty
    [DataRow("", "Default", 100, "name")]
    // Name is too short
    [DataRow("X", "Default", 100, "name")]
    // Name is too long
    [DataRow("012345678901234567890123456789012345678901234567891", "Default", 100, "name")]
    // Slot name is null
    [DataRow("General Admission", null, 100, "slotName")]
    // Slot name is empty
    [DataRow("General Admission", "", 100, "slotName")]
    // Slot name is too short
    [DataRow("General Admission", "X", 100, "slotName")]
    // Slot name is too long
    [DataRow("General Admission", "012345678901234567890123456789012345678901234567891", 100, "slotName")]
    // Max capacity is empty
    [DataRow("General Admission", "Default", 0, "maxCapacity")]
    public async Task TicketTypeIsInvalid_ReturnsBadRequest(string name, string slotName, int maxCapacity,
        string expectedErrorKey)
    {
        // Arrange
        var ticketType = new TicketTypeDto(name, slotName, maxCapacity);
        
        var request = new CreateTicketedEventRequestBuilder()
            .WithTicketTypes([ticketType])
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey($"ticketTypes[0].{expectedErrorKey}"));
    }
    

    [DoNotParallelize]
    [TestClass]
    public class FullStackTests : FullStackTestsBase
    {
        [TestMethod]
        public async Task ValidEvent_CreatesEvent()
        {
            // Arrange
            var request = new CreateTicketedEventRequestBuilder().Build();

            // Act
            var response = await ApiClient.PostAsJsonAsync($"/events/v1", request);
               
            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
            var result = await response.Content.ReadFromJsonAsync<CreateTicketedEventResponse>();
            (result?.Id).ShouldNotBeNull();

            var createdEvent = await Database.Context.TicketedEvents.FindAsync(result.Id);
            createdEvent.ShouldNotBeNull();
        }
        
        // TODO Verify that e-mail templates are set.
    }
}
