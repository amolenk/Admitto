namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Gets a ticketed event.
/// </summary>
public class GetTicketedEventHandler(IDomainContext context) 
    : IQueryHandler<GetTicketedEventQuery, GetTicketedEventResult?>
{
    public async ValueTask<GetTicketedEventResult?> HandleAsync(GetTicketedEventQuery query, 
        CancellationToken cancellationToken)
    {
        var team = await context.Teams.GetByIdAsync(query.TeamId, cancellationToken);

        var ticketedEvent = team.ActiveEvents.FirstOrDefault(e => e.Id == query.TicketedEventId);

        return ticketedEvent is not null ? GetTicketedEventResult.FromTicketedEvent(ticketedEvent) : null;
    }
}
