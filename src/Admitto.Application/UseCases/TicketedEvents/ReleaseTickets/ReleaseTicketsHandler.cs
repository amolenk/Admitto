using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReleaseTickets;

/// <summary>
/// Represents a command handler that processes the releases of tickets from a ticketed event.
/// </summary>
public class ReleaseTicketsHandler(IApplicationContext context) : IWorkerCommandHandler<ReleaseTicketsCommand>
{
    public async ValueTask HandleAsync(ReleaseTicketsCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents
            .FirstOrDefaultAsync(te => te.Id == command.TicketedEventId, cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }
 
        ticketedEvent.ReleaseTickets(command.Tickets);
    }
}
