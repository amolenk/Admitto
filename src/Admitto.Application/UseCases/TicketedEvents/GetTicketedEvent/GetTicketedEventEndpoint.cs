using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Get a specific ticketed event by its ID.
/// </summary>
public static class GetTicketedEventEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEvent(this RouteGroupBuilder group)
    {
        group.MapGet("/{ticketedEventId:guid}", GetTicketedEvent);

        return group;
    }
    
    private static async ValueTask<Results<Ok<GetTicketedEventResponse>, BadRequest<string>, NotFound<string>>> GetTicketedEvent(
        Guid teamId, Guid ticketedEventId, IDomainContext context, CancellationToken cancellationToken)
    {
        var team = await context.Teams.FindAsync([teamId], cancellationToken);
        if (team is null)
        {
            return TypedResults.BadRequest(Error.TeamNotFound(teamId));
        }

        throw new NotImplementedException();

        // var ticketedEvent = team.ActiveEvents.FirstOrDefault(e => e.Id == ticketedEventId);
        //
        // return ticketedEvent is not null 
        //     ? TypedResults.Ok(GetTicketedEventResponse.FromTicketedEvent(ticketedEvent))
        //     : TypedResults.NotFound(Error.TicketedEventNotFound(ticketedEventId));
    }
}
