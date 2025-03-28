namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetActiveEvents;

/// <summary>
/// Gets all the active events for a team.
/// </summary>
public class GetActiveEventsHandler(IDomainContext context) 
    : IQueryHandler<GetActiveEventsQuery, GetActiveEventsResult>
{
    public async ValueTask<GetActiveEventsResult> HandleAsync(GetActiveEventsQuery query, 
        CancellationToken cancellationToken)
    {
        var team = await context.Teams.GetByIdAsync(query.TeamId, cancellationToken);

        return GetActiveEventsResult.FromTicketedEvents(team.ActiveEvents);
    }
}
