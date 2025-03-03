using Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Features.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public class CreateTicketedEventHandler(IApplicationDbContext dbContext) 
    : ICommandHandler<CreateTicketedEventCommand, CreateTicketedEventResult>
{
    public async ValueTask<CreateTicketedEventResult> HandleAsync(CreateTicketedEventCommand request, 
        CancellationToken cancellationToken)
    {
        var ticketedEvent = TicketedEvent.Create(request.Name, request.StartDay, request.EndDay,
            request.SalesStartDateTime, request.SalesEndDateTime);
        
        foreach (var ticketTypeDto in request.TicketTypes ?? [])
        {
            var ticketType = TicketType.Create(ticketTypeDto.Name, ticketTypeDto.StartDateTime,
                ticketTypeDto.EndDateTime, ticketTypeDto.MaxCapacity);
            
            ticketedEvent.AddTicketType(ticketType);
        }
        
        dbContext.TicketedEvents.Add(ticketedEvent);

        var command = new ReserveTicketsCommand(Guid.NewGuid());
        dbContext.Outbox.Add(OutboxMessage.FromCommand(command));
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return new CreateTicketedEventResult(ticketedEvent.Id);
    }
}
