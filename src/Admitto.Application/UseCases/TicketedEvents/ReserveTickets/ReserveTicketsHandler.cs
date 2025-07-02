using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;

/// <summary>
/// Reserves the required tickets for a registration.
/// </summary>
public class ReserveTicketsHandler(IDomainContext context) : ICommandHandler<ReserveTicketsCommand>
{
    public async ValueTask HandleAsync(ReserveTicketsCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents.FindAsync([command.TicketedEventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw ValidationError.TicketedEvent.NotFound(command.TicketedEventId);
        }
        
        var ticketQuantities = command.Tickets
            .Select(t => new TicketQuantity(t.Key, t.Value));
        
        ticketedEvent.ReserveTickets(command.RegistrationId, ticketQuantities);
    }
}
