using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.AddBadgeInstance.AdminApi;

public static class AddBadgeInstanceHttpEndpoint
{
    public static RouteGroupBuilder MapAddBadgeInstance(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{badgeTypeId:guid}/instances", AddBadgeInstance)
            .WithName(nameof(AddBadgeInstance))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created<AddBadgeInstanceHttpResponse>> AddBadgeInstance(
        Guid teamId,
        Guid eventId,
        Guid badgeTypeId,
        AddBadgeInstanceHttpRequest request,
        ICommandHandler<AddBadgeInstanceCommand, Guid> handler,
        [FromKeyedServices(BadgesModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(request.ToCommand(eventId, teamId, badgeTypeId), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/teams/{teamId}/events/{eventId}/badge-types/{badgeTypeId}/instances/{id}",
            new AddBadgeInstanceHttpResponse(id));
    }
}
