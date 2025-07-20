using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Get a specific ticketed event.
/// </summary>
public static class GetTicketedEventEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{eventSlug}", GetTicketedEvent)
            .WithName(nameof(GetTicketedEvent))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }
    
    private static async ValueTask<Results<Ok<GetTicketedEventResponse>, NotFound>> GetTicketedEvent(string teamSlug, 
        string eventSlug, IDomainContext context, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents
            .AsNoTracking()
            .Join(context.Teams,
                e => e.TeamId,
                t => t.Id,
                (e, t) => new { Event = e, Team = t })
            .Where(joined => joined.Event.Slug == eventSlug && joined.Team.Slug == teamSlug)
            .Select(joined => joined.Event)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (ticketedEvent is null)
        {
            return TypedResults.NotFound();
        }
        
        var ticketTypes = ticketedEvent.TicketTypes.Select(t => new TicketTypeDto(
            t.Slug, t.Name, t.SlotName, t.MaxCapacity, t.UsedCapacity));
        
        var response = new GetTicketedEventResponse(ticketedEvent.Slug, ticketedEvent.Name,
            ticketedEvent.StartTime, ticketedEvent.EndTime, ticketedEvent.RegistrationStartTime, 
            ticketedEvent.RegistrationEndTime, ticketTypes);

        return TypedResults.Ok(response);
    }
}
