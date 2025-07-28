using Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;
using Amolenk.Admitto.Domain;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;
using FluentValidation;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Attendees.RegisterAttendee;

[DoNotParallelize]
[TestClass]
public class RegisterAttendeeTests : FullStackTestsBase
{
    private const string RequestUri = "/registrations/v1";

    private Team _testTeam = null!;
    private TicketedEvent _testEvent = null!;
    
    [TestInitialize]
    public override async Task TestInitialize()
    {
        await base.TestInitialize();

        await SeedDatabaseAsync(context =>
        {
            _testTeam = new TeamBuilder()
                .WithEmailSettings(Email.DefaultEmailSettings)
                .Build();
            
            _testEvent = new TicketedEventBuilder()
                .WithTeamId(_testTeam.Id)
                .Build();

            context.Teams.Add(_testTeam);
            context.TicketedEvents.Add(_testEvent);
        });
    }
    
    [TestMethod]
    public async Task NewRegistration_TicketedEventDoesNotExist_ReturnsBadRequest()
    {
        // Arrange
        var ticketedEventId = Guid.NewGuid();
        var request = new RegisterAttendeeRequestBuilder()
            .WithTicketedEventId(ticketedEventId)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);
    
        // Assert
        await response.ShouldBeBadRequestAsync(ErrorMessage.TicketedEvent.NotFound(ticketedEventId));
    }
    
    [TestMethod]
    public async Task NewRegistration_TicketTypeHasNoCapacity_ReturnsBadRequest()
    {
        // Arrange
        var ticketType = _testEvent.TicketTypes.First();
        var tickets = new Dictionary<Guid, int>()
        {
            [ticketType.Id] = ticketType.UsedCapacity + 1 // Request more tickets than available
        };
        
        var request = new RegisterAttendeeRequestBuilder()
            .WithTicketedEventId(_testEvent.Id)
            .WithTickets(tickets)
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);
    
        // Assert
        await response.ShouldBeBadRequestAsync(problem =>
        {
            problem.Detail.ShouldBe(ErrorMessage.TicketedEvent.SoldOut);
        });
    }
    
    [TestMethod]
    public async Task NewRegistration_CreatesRegistration()
    {
        // Arrange
        var ticketTypeId = _testEvent.TicketTypes.First().Id;
        
        var request = new RegisterAttendeeRequestBuilder()
            .WithTicketedEventId(_testEvent.Id)
            .WithTickets(new Dictionary<Guid, int> {
                [ticketTypeId] = 1
            })
            .Build();
    
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);
           
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    
        var result = await response.Content.ReadFromJsonAsync<RegisterAttendeeResponse>();
        (result?.Id).ShouldNotBeNull();
    
        var createdRegistration = await Database.Context.Registrations.FindAsync(result.Id);
        createdRegistration.ShouldSatisfyAllConditions(registration =>
        {
            registration.ShouldNotBeNull();
            registration.Tickets.ShouldSatisfyAllConditions(tickets =>
            {
                tickets.ShouldNotBeNull().Count.ShouldBe(1);
                tickets.First().ShouldSatisfyAllConditions(ticket =>
                {
                    ticket.TicketTypeId.Value.ShouldBe(ticketTypeId);
                    ticket.Quantity.ShouldBe(1);
                });
            });
        });
    }
    
    // [TestMethod]
    // public async Task NewRegistration_EnqueuesReserveTicketsCommand()
    // {
    //     // Arrange
    //     var ticketTypeId = _testEvent.TicketTypes.First().Id;
    //     
    //     var request = new RegisterAttendeeRequestBuilder()
    //         .WithTicketedEventId(_testEvent.Id)
    //         .WithTickets(new Dictionary<Guid, int> {
    //             [ticketTypeId] = 1
    //         })
    //         .Build();
    //
    //     // Act
    //     var response = await ApiClient.PostAsJsonAsync(RequestUri, request);
    //
    //     // Assert
    //     var result = await response.Content.ReadFromJsonAsync<RegisterAttendeeResponse>();
    //
    //     await QueueStorage.MessageQueue.ShouldContainMessageAsync<HoldTicketsCommand>(message =>
    //     {
    //         message.RegistrationId.ShouldBe(result!.Id);
    //     });
    // }
    //
    // [TestMethod]
    // public async ValueTask ReserveTickets_RegistrationNotFound_ThrowsValidationException()
    // {
    //     // Arrange
    //     var registrationId = Guid.Empty;
    //     var command = new HoldTicketsCommand(registrationId);
    //
    //     // Act & Assert
    //     var exception = await Should.ThrowAsync<ValidationException>(
    //         async () => await HandleCommand<HoldTicketsCommand, HoldTicketsHandler>(command));
    //     
    //     exception.Message.ShouldBe(ErrorMessage.AttendeeRegistration.NotFound(registrationId));
    // }
    //
    // [TestMethod]
    // public async ValueTask ReserveTickets_TicketedEventNotFound_ThrowsValidationException()
    // {
    //     // Arrange
    //     var ticketedEventId = Guid.Empty;
    //     var registration = new AttendeeRegistrationBuilder()
    //         .WithTicketedEventId(ticketedEventId)
    //         .Build();
    //
    //     await SeedDatabaseAsync(context =>
    //     {
    //         context.AttendeeRegistrations.Add(registration);
    //     });
    //     
    //     var command = new HoldTicketsCommand(registration.Id);
    //
    //     // Act & Assert
    //     var exception = await Should.ThrowAsync<ValidationException>(
    //         async () => await HandleCommand<HoldTicketsCommand, HoldTicketsHandler>(command));
    //     
    //     exception.Message.ShouldBe(ErrorMessage.TicketedEvent.NotFound(ticketedEventId));
    // }
    //
    // [TestMethod]
    // public async ValueTask ReserveTickets_()
    // {
    //     // Arrange
    //     var registration = new AttendeeRegistrationBuilder()
    //         .WithTicketedEventId(_testEvent.Id)
    //         .WithTickets(new Dictionary<TicketTypeId, int>
    //         {
    //             [_testEvent.TicketTypes.First().Id] = 1
    //         })
    //         .Build();
    //
    //     await SeedDatabaseAsync(context =>
    //     {
    //         context.AttendeeRegistrations.Add(registration);
    //     });
    //     
    //     var command = new HoldTicketsCommand(registration.Id);
    //
    //     // Act
    //     await HandleCommand<HoldTicketsCommand, HoldTicketsHandler>(command);
    //     
    //     // Assert
    //     // ???
    // }
}

