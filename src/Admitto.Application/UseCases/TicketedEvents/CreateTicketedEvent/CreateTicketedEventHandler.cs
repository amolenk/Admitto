using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public class CreateTicketedEventHandler(IApplicationContext context) 
    : ICommandHandler<CreateTicketedEventCommand, CreateTicketedEventResult>
{
    public async ValueTask<CreateTicketedEventResult> HandleAsync(CreateTicketedEventCommand command, 
        CancellationToken cancellationToken)
    {
        var ticketedEvent = TicketedEvent.Create(command.Name, command.StartDay, command.EndDay,
            command.SalesStartDateTime, command.SalesEndDateTime);
        
        foreach (var ticketTypeDto in command.TicketTypes ?? [])
        {
            var ticketType = TicketType.Create(ticketTypeDto.Name, ticketTypeDto.StartDateTime,
                ticketTypeDto.EndDateTime, ticketTypeDto.MaxCapacity);
            
            ticketedEvent.AddTicketType(ticketType);
        }
        
        context.TicketedEvents.Add(ticketedEvent);

        await context.SaveChangesAsync(cancellationToken);
        
        return new CreateTicketedEventResult(ticketedEvent.Id);
    }
}
