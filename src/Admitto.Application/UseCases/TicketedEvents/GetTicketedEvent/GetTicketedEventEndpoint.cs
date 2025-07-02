namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Get a specific ticketed event by its ID.
/// </summary>
public static class GetTicketedEventEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{ticketedEventId:guid}", GetTicketedEvent)
            .Produces<Ok<GetTicketedEventResponse>>()
            .Produces<NotFound>();

        return group;
    }
    
    private static async ValueTask<IResult> GetTicketedEvent(Guid ticketedEventId, IDomainContext context,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents.FindAsync([ticketedEventId], cancellationToken);

        return ticketedEvent is not null 
            ? TypedResults.Ok(GetTicketedEventResponse.FromTicketedEvent(ticketedEvent))
            : TypedResults.NotFound();
    }
}
