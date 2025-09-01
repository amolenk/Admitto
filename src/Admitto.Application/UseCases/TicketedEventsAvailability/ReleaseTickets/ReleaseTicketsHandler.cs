using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.TicketedEventsAvailability.ReleaseTickets;

/// <summary>
/// Represents a command handler that processes the releases of tickets from a ticketed event.
/// </summary>
public class ReleaseTicketsHandler(IApplicationContext context) : ICommandHandler<ReleaseTicketsCommand>
{
    public async ValueTask HandleAsync(ReleaseTicketsCommand command, CancellationToken cancellationToken)
    {
        var availability = await context.TicketedEventAvailability.SingleOrDefaultAsync(
            tea => tea.TicketedEventId == command.TicketedEventId,
            cancellationToken);
 
        if (availability is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        availability.ReleaseTickets(command.Tickets);
    }
}