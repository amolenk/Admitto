using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.GetEventCreationRequest.AdminApi;

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
        IQueryHandler<GetEventCreationRequestQuery, EventCreationRequestDto> handler,
        CancellationToken cancellationToken)
    {
        var dto = await handler.HandleAsync(new GetEventCreationRequestQuery(teamId, creationRequestId), cancellationToken);

        return TypedResults.Ok(dto);
    }
}
