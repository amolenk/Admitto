using System.Security.Claims;
using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Data;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvents;

/// <summary>
/// Get all ticketed events for a team.
/// </summary>
public static class GetTicketedEventsEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEvents(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTicketedEvents)
            .WithName(nameof(GetTicketedEvents));

        return group;
    }
    
    private static async ValueTask<Results<Ok<GetTicketedEventsResponse>, UnauthorizedHttpResult>> GetTicketedEvents(
        string teamSlug, IDomainContext context, ClaimsPrincipal principal, IAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }
        
        var authorizedEvents = (
                await authorizationService.GetTicketedEventsAsync(userId.Value, teamSlug, cancellationToken))
            .ToList();

        if (authorizedEvents.Count == 0)
        {
            return TypedResults.Ok(new GetTicketedEventsResponse([]));
        }
        
        var teamId = await context.Teams.GetTeamIdAsync(teamSlug, cancellationToken);

        // TODO Only select required fields to improve performance
        var ticketedEvents = await context.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && authorizedEvents.Contains(e.Slug))
            .ToListAsync(cancellationToken);
        
        var response = new GetTicketedEventsResponse(ticketedEvents
            .Select(e => new TicketedEventDto(e.Slug, e.Name, e.StartTime, e.EndTime, 
                e.RegistrationStartTime, e.RegistrationEndTime))
            .ToArray());
        
        return TypedResults.Ok(response);
    }
}