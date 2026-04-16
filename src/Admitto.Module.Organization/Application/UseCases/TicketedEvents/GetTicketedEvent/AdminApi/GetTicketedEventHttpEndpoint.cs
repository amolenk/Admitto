using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvent.AdminApi;

public static class GetTicketedEventHttpEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTicketedEvent)
            .WithName(nameof(GetTicketedEvent))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<TicketedEventDto>> GetTicketedEvent(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var query = new GetTicketedEventQuery(scope.TeamId, eventSlug);

        var ticketedEvent = await mediator.QueryAsync<GetTicketedEventQuery, TicketedEventDto>(
            query, cancellationToken);

        return TypedResults.Ok(ticketedEvent);
    }
}
