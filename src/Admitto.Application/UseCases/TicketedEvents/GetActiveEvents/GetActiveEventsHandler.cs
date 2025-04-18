namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetActiveEvents;

/// <summary>
/// Gets all the active events for a team.
/// </summary>
public class GetActiveEventsHandler(IDomainContext context) 
    : IQueryHandler<GetActiveEventsQuery, IEnumerable<TicketedEventDto>>
{
    public async ValueTask<Result<IEnumerable<TicketedEventDto>>> HandleAsync(GetActiveEventsQuery query, 
        CancellationToken cancellationToken)
    {
        var team = await context.Teams.FindAsync([query.TeamId], cancellationToken);
        if (team is null)
        {
            return Result<IEnumerable<TicketedEventDto>>.Failure("Team not found.");
        }
        
        return Result<IEnumerable<TicketedEventDto>>.Success(
            team.ActiveEvents.Select(TicketedEventDto.FromTicketedEvent));
    }
}
