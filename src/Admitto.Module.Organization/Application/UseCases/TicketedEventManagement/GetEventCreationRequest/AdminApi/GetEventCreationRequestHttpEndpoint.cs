using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest.AdminApi;

/// <summary>
/// GET /admin/teams/{teamSlug}/event-creations/{creationRequestId} — surfaces the status
/// of an asynchronous ticketed-event creation request.
/// </summary>
public static class GetEventCreationRequestHttpEndpoint
{
    public static RouteGroupBuilder MapGetEventCreationRequest(this RouteGroupBuilder group)
    {
        group
            .MapGet("/event-creations/{creationRequestId:guid}", GetEventCreationRequest)
            .WithName(nameof(GetEventCreationRequest))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<EventCreationRequestDto>> GetEventCreationRequest(
        string teamSlug,
        Guid creationRequestId,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, cancellationToken: cancellationToken);

        var query = new GetEventCreationRequestQuery(scope.TeamId, creationRequestId);

        var dto = await mediator.QueryAsync<GetEventCreationRequestQuery, EventCreationRequestDto>(
            query, cancellationToken);

        return TypedResults.Ok(dto);
    }
}
