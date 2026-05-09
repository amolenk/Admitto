using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation.AdminApi;

/// <summary>
/// POST /admin/teams/{teamId}/event-creations — kicks off an asynchronous ticketed-event
/// creation. Returns 202 Accepted with a <c>Location</c> header pointing at the
/// creation-status endpoint.
/// </summary>
public static class RequestTicketedEventCreationHttpEndpoint
{
    public static RouteGroupBuilder MapRequestTicketedEventCreation(this RouteGroupBuilder group)
    {
        group
            .MapPost("/events", RequestTicketedEventCreation)
            .WithName(nameof(RequestTicketedEventCreation))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Accepted> RequestTicketedEventCreation(
        Guid teamId,
        IUserContextAccessor userContextAccessor,
        RequestTicketedEventCreationHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(OrganizationModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(teamId, userContextAccessor.Current.UserId);

        var creationRequestId = await mediator.SendReceiveAsync<RequestTicketedEventCreationCommand, Guid>(
            command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Accepted($"/admin/teams/{teamId}/event-creations/{creationRequestId}");
    }
}
