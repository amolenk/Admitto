using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.DataAccess;

public static class TicketedEventDataExtensions
{
    public static async ValueTask<TicketedEvent> GetByIdAsync(this DbSet<TicketedEvent> ticketedEvents, Guid id,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await ticketedEvents.FindAsync([id], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new TicketedEventNotFoundException(id);
        }
        
        return ticketedEvent;
    }
}