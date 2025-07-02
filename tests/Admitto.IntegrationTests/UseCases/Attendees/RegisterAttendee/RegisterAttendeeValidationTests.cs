using Amolenk.Admitto.Domain;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Attendees.RegisterAttendee;

[TestClass]
public class RegisterAttendeeValidationTests : ApiTestsBase
{
    private const string RequestUri = "/registrations/v1";

    [DataTestMethod]
    [DataRow(null)]
    public async Task TicketedEventIdIsInvalid_ReturnsBadRequest(Guid ticketedEventId)
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithTicketedEventId(ticketedEventId)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync(nameof(request.TicketedEventId), 
            ErrorMessage.TicketedEvent.Id.MustNotBeEmpty);
    }
    
    [DataTestMethod]
    [DataRow(null, ErrorMessage.Attendee.Email.IsRequired)]
    [DataRow("not-an-email", ErrorMessage.Attendee.Email.MustBeValid)]
    public async Task EmailIsInvalid_ReturnsBadRequest(string? email, string expectedError)
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithEmail(email!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync(nameof(request.Email), expectedError);
    }
    
    [DataTestMethod]
    [DataRow(null, ErrorMessage.Attendee.FirstName.IsRequired)]
    [DataRow("", ErrorMessage.Attendee.FirstName.MustBeMin2Length)]
    [DataRow("X", ErrorMessage.Attendee.FirstName.MustBeMin2Length)]
    [DataRow("012345678901234567890123456789012345678901234567891", ErrorMessage.Attendee.FirstName.MustBeMax50Length)]
    public async Task FirstNameIsInvalid_ReturnsBadRequest(string? firstName, string expectedError)
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithFirstName(firstName!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync(nameof(request.FirstName), expectedError);
    }

    [DataTestMethod]
    [DataRow(null, ErrorMessage.Attendee.LastName.IsRequired)]
    [DataRow("", ErrorMessage.Attendee.LastName.MustBeMin2Length)]
    [DataRow("X", ErrorMessage.Attendee.LastName.MustBeMin2Length)]
    [DataRow("012345678901234567890123456789012345678901234567891", ErrorMessage.Attendee.LastName.MustBeMax50Length)]
    public async Task LastNameIsInvalid_ReturnsBadRequest(string? lastName, string expectedError)
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithLastName(lastName!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync(nameof(request.LastName), expectedError);
    }

    [TestMethod]
    public async Task DetailsAreNull_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithDetails(null!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync(nameof(request.Details), ErrorMessage.Attendee.Details.AreRequired);
    }
    
    [DataTestMethod]
    [DataRow("", ErrorMessage.Attendee.Details.Key.MustNotBeEmpty)]
    [DataRow("012345678901234567890123456789012345678901234567891", ErrorMessage.Attendee.Details.Key.MustBeMax50Length)]
    public async Task DetailKeyIsInvalid_ReturnsBadRequest(string key, string expectedError)
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithDetails(new Dictionary<string, string>
            {
                [key] = "bar"
            })
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync($"{nameof(request.Details)}[0].Key", expectedError);
    }
    
    [DataTestMethod]
    [DataRow(null, ErrorMessage.Attendee.Details.Value.IsRequired)]
    [DataRow("012345678901234567890123456789012345678901234567891", ErrorMessage.Attendee.Details.Value.MustBeMax50Length)]
    public async Task DetailValueIsInvalid_ReturnsBadRequest(string value, string expectedError)
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithDetails(new Dictionary<string, string>
            {
                ["foo"] = value
            })
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync($"{nameof(request.Details)}[0].Value", expectedError);
    }
    
    [TestMethod]
    public async Task TicketTypesIsNull_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithTickets(null!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync(nameof(request.Tickets), 
            ErrorMessage.AttendeeRegistration.Tickets.AreRequired);
    }

    [TestMethod]
    public async Task TicketTypesIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithTickets([])
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync(nameof(request.Tickets),
            ErrorMessage.AttendeeRegistration.Tickets.MustNotBeEmpty);
    }
    
    [DataTestMethod]
    [DataRow(null, ErrorMessage.AttendeeRegistration.Tickets.TicketType.MustNotBeEmpty)]
    public async Task TicketTicketTypeIdIsInvalid_ReturnsBadRequest(Guid ticketTypeId, string expectedError)
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithTickets(new Dictionary<Guid, int>
            {
                [ticketTypeId] = 1
            })
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync($"{nameof(request.Tickets)}[0].Key", expectedError);
    }
    
    [DataTestMethod]
    [DataRow(0, ErrorMessage.AttendeeRegistration.Tickets.Quantity.MustNotBeEmpty)]
    [DataRow(-1, ErrorMessage.AttendeeRegistration.Tickets.Quantity.MustBeGreaterThanZero)]
    public async Task TicketQuantityIsInvalid_ReturnsBadRequest(int value, string expectedError)
    {
        // Arrange
        var request = new RegisterAttendeeRequestBuilder()
            .WithTickets(new Dictionary<Guid, int>
            {
                [Guid.NewGuid()] = value
            })
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestAsync($"{nameof(request.Tickets)}[0].Value", expectedError);
    }
}