using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Features.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public class CreateTicketedEventHandler(IApplicationDbContext dbContext) 
    : ICommandHandler<CreateTicketedEventCommand, CreateTicketedEventResult>
{
    public async ValueTask<CreateTicketedEventResult> HandleAsync(CreateTicketedEventCommand request, CancellationToken cancellationToken)
    {
        var ticketedEvent = TicketedEvent.Create(request.Name, request.StartDay, request.EndDay,
            request.SalesStartDateTime, request.SalesEndDateTime);
        
        foreach (var ticketTypeDto in request.TicketTypes ?? [])
        {
            var ticketType = TicketType.Create(ticketTypeDto.Name, ticketTypeDto.StartDateTime,
                ticketTypeDto.EndDateTime, ticketTypeDto.MaxCapacity);
            
            Console.WriteLine("Ticket type: " + ticketType.Id);
            
            ticketedEvent.AddTicketType(ticketType);
        }

        dbContext.TicketedEvents.Add(ticketedEvent);

        await dbContext.SaveChangesAsync(cancellationToken);
        
        // await ticketedEventRepository.SaveChangesAsync(
        //     ticketedEvent,
        //     outboxMessages: ticketedEvent.GetDomainEvents().Select(OutboxMessage.FromDomainEvent));
        
        return new CreateTicketedEventResult(ticketedEvent.Id);
    }
}

// public class CreateTicketedEventHandler(ITicketedEventRepository ticketedEventRepository) 
//     : ICommandHandler<CreateTicketedEventCommand, CreateTicketedEventResult>
// {
//     public async ValueTask<CreateTicketedEventResult> HandleAsync(CreateTicketedEventCommand request, CancellationToken cancellationToken)
//     {
//         var ticketedEvent = TicketedEvent.Create(request.Name, request.StartDay, request.EndDay,
//             request.SalesStartDateTime, request.SalesEndDateTime);
//         
//         foreach (var ticketTypeDto in request.TicketTypes ?? [])
//         {
//             var ticketType = TicketType.Create(ticketTypeDto.Name, ticketTypeDto.StartDateTime,
//                 ticketTypeDto.EndDateTime, ticketTypeDto.MaxCapacity);
//             
//             Console.WriteLine("Ticket type: " + ticketType.Id);
//             
//             ticketedEvent.AddTicketType(ticketType);
//         }
//         
//         await ticketedEventRepository.SaveChangesAsync(
//             ticketedEvent,
//             outboxMessages: ticketedEvent.GetDomainEvents().Select(OutboxMessage.FromDomainEvent));
//         
//         return new CreateTicketedEventResult(ticketedEvent.Id);
//     }
// }
