using System.Security.Claims;

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
        string teamSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        ClaimsPrincipal principal,
        IAuthorizationService authorizationService,
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

        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);

        var ticketedEvents = await context.TicketedEvents
            .AsNoTracking()
            .Where(te => te.TeamId == teamId && authorizedEvents.Contains(te.Slug))
            .Select(te => new TicketedEventDto(
                te.Slug,
                te.Name,
                te.StartsAt,
                te.EndsAt,
                te.RegistrationOpensAt,
                te.RegistrationClosesAt))
            .ToArrayAsync(cancellationToken);
        
        return TypedResults.Ok(new GetTicketedEventsResponse(ticketedEvents));
    }
}