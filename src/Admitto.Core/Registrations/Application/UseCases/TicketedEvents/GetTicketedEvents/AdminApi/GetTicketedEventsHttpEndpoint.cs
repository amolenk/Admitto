using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEvents.AdminApi;

/// <summary>
/// GET /admin/teams/{teamSlug}/events — lists the team's ticketed events
/// (active and cancelled; archived events are excluded).
/// </summary>
public static class GetTicketedEventsHttpEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEvents(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTicketedEvents)
            .WithName(nameof(GetTicketedEvents))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<TicketedEventListItemDto>>> GetTicketedEvents(
        Guid teamId,
        IQueryHandler<GetTicketedEventsQuery, IReadOnlyList<TicketedEventListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTicketedEventsQuery(TeamId.From(teamId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
