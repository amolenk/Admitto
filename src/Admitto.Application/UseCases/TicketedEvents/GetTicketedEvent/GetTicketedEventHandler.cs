namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Gets a ticketed event.
/// </summary>
public class GetTicketedEventHandler(IDomainContext context) 
    : IQueryHandler<GetTicketedEventQuery, TicketedEventDto>
{
    public async ValueTask<Result<TicketedEventDto>> HandleAsync(GetTicketedEventQuery query, 
        CancellationToken cancellationToken)
    {
        var team = await context.Teams.FindAsync([query.TeamId], cancellationToken);
        if (team is null)
        {
            return Result<TicketedEventDto>.Failure("Team not found.");
        }
        
        var ticketedEvent = team.ActiveEvents.FirstOrDefault(e => e.Id == query.TicketedEventId);

        return ticketedEvent is not null 
            ? Result<TicketedEventDto>.Success(TicketedEventDto.FromTicketedEvent(ticketedEvent))
            : Result<TicketedEventDto>.NotFound();
    }
}
