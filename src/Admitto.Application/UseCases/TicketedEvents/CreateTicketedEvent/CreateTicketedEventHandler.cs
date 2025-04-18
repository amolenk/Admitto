using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public class CreateTicketedEventHandler(IDomainContext context) 
    : ICommandHandler<CreateTicketedEventCommand, Guid>
{
    public async ValueTask<Result<Guid>> HandleAsync(CreateTicketedEventCommand command, 
        CancellationToken cancellationToken)
    {
        var team = await context.Teams.FindAsync([command.TeamId], cancellationToken);
        if (team is null)
        {
            return Result<Guid>.Failure("Team not found.");
        }
        
        var newEvent = TicketedEvent.Create(command.Name, command.StartDay, command.EndDay,
            command.SalesStartDateTime, command.SalesEndDateTime);
        
        foreach (var ticketTypeDto in command.TicketTypes ?? [])
        {
            var ticketType = TicketType.Create(ticketTypeDto.Name, ticketTypeDto.StartDateTime,
                ticketTypeDto.EndDateTime, ticketTypeDto.MaxCapacity);
            
            newEvent.AddTicketType(ticketType);
        }

        team.AddActiveEvent(newEvent);
        
        return Result<Guid>.Success(newEvent.Id);
    }
}
