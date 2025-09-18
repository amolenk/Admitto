using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReleaseTickets;

/// <summary>
/// Represents a command handler that processes the releases of tickets from a ticketed event.
/// </summary>
public class ReleaseTicketsHandler(IApplicationContext context) : ICommandHandler<ReleaseTicketsCommand>
{
    public async ValueTask HandleAsync(ReleaseTicketsCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(te => te.Id == command.TicketedEventId, cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }
 
        ticketedEvent.ReleaseTickets(command.Tickets);
    }
}
