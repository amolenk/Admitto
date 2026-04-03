using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvents.AdminApi;

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

    private static async ValueTask<Ok<TicketedEventListItemDto[]>> GetTicketedEvents(
        OrganizationScope organizationScope,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetTicketedEventsQuery(organizationScope.TeamId);

        var events = await mediator.QueryAsync<GetTicketedEventsQuery, TicketedEventListItemDto[]>(
            query, cancellationToken);

        return TypedResults.Ok(events);
    }
}
