namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Gets a ticketed event.
/// </summary>
public class GetTicketedEventHandler(IApplicationContext context) 
    : IQueryHandler<GetTicketedEventQuery, GetTicketedEventResult?>
{
    public async ValueTask<GetTicketedEventResult?> HandleAsync(GetTicketedEventQuery query, 
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents.FindAsync([query.Id, cancellationToken],
            cancellationToken: cancellationToken);

        return ticketedEvent is not null ? GetTicketedEventResult.FromTicketedEvent(ticketedEvent) : null;
    }
}
