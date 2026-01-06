using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

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
            .WithName(nameof(GetTicketedEvents))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Crew));

        return group;
    }

    private static async ValueTask<Results<Ok<GetTicketedEventsResponse>, UnauthorizedHttpResult>> GetTicketedEvents(
        string teamSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);
        
        var ticketedEvents = await context.TicketedEvents
            .AsNoTracking()
            .Where(te => te.TeamId == teamId)
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