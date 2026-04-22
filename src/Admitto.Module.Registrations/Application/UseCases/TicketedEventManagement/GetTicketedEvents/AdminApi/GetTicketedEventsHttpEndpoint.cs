using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents.AdminApi;

/// <summary>
/// GET /admin/teams/{teamSlug}/events — lists the team's ticketed events
/// (active, cancelled and archived).
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
        string teamSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, cancellationToken: cancellationToken);

        var query = new GetTicketedEventsQuery(TeamId.From(scope.TeamId));

        var result = await mediator.QueryAsync<GetTicketedEventsQuery, IReadOnlyList<TicketedEventListItemDto>>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
