using Amolenk.Admitto.Domain;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetActiveTicketedEvents;

/// <summary>
/// Get all ticketed events that a team is actively organizing.
/// </summary>
public static class GetActiveTicketedEventsEndpoint
{
    public static RouteGroupBuilder MapGetActiveTicketedEvents(this RouteGroupBuilder group)
    {
        group.MapGet("/active", GetActiveTicketedEvents);

        return group;
    }
    
    private static async ValueTask<Results<Ok<GetActiveTicketedEventsResponse>, BadRequest<string>>> GetActiveTicketedEvents(
        Guid teamId, IDomainContext context, CancellationToken cancellationToken)
    {
        var team = await context.Teams.FindAsync([teamId], cancellationToken);
        if (team is null)
        {
            throw ValidationError.Team.NotFound(teamId);
        }
        
        throw new NotImplementedException();

        // return TypedResults.Ok(GetActiveTicketedEventsResponse.FromTicketedEvents(team.ActiveEvents));
    }
}