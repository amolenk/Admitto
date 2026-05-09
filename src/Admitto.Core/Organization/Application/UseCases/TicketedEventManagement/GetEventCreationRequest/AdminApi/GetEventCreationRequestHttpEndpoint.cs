using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest.AdminApi;

/// <summary>
/// GET /admin/teams/{teamId}/event-creations/{creationRequestId} — surfaces the status
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
        Guid teamId,
        Guid creationRequestId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetEventCreationRequestQuery(teamId, creationRequestId);

        var dto = await mediator.QueryAsync<GetEventCreationRequestQuery, EventCreationRequestDto>(
            query, cancellationToken);

        return TypedResults.Ok(dto);
    }
}
