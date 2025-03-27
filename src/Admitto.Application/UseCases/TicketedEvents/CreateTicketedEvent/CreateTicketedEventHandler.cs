using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public class CreateTicketedEventHandler(IDomainContext context) 
    : ICommandHandler<CreateTicketedEventCommand, CreateTicketedEventResult>
{
    public async ValueTask<CreateTicketedEventResult> HandleAsync(CreateTicketedEventCommand command, 
        CancellationToken cancellationToken)
    {
        var team = await context.Teams.GetByIdAsync(command.TeamId, cancellationToken);
        
        var newEvent = TicketedEvent.Create(command.Name, command.StartDay, command.EndDay,
            command.SalesStartDateTime, command.SalesEndDateTime);
        
        foreach (var ticketTypeDto in command.TicketTypes ?? [])
        {
            var ticketType = TicketType.Create(ticketTypeDto.Name, ticketTypeDto.StartDateTime,
                ticketTypeDto.EndDateTime, ticketTypeDto.MaxCapacity);
            
            newEvent.AddTicketType(ticketType);
        }

        team.AddActiveEvent(newEvent);
        
        return new CreateTicketedEventResult(newEvent.Id);
    }
}
