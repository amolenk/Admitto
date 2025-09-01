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
            .Join(
                context.TicketedEventAvailability,
                te => te.Id,
                tea => tea.TicketedEventId,
                (te, tea) => new { Event = te, Availability = tea })
            .Where(x => x.Event.TeamId == teamId && authorizedEvents.Contains(x.Event.Slug))
            .Select(x => new TicketedEventDto(
                x.Event.Slug,
                x.Event.Name,
                x.Event.StartTime,
                x.Event.EndTime,
                x.Availability.RegistrationStartTime,
                x.Availability.RegistrationEndTime))
            .ToArrayAsync(cancellationToken);
        
        return TypedResults.Ok(new GetTicketedEventsResponse(ticketedEvents));
    }
}