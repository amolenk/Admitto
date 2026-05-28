using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance.AdminApi;

public static class UpdateBadgeInstanceHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateBadgeInstance(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{badgeTypeId:guid}/instances/{badgeInstanceId:guid}", UpdateBadgeInstance)
            .WithName(nameof(UpdateBadgeInstance))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> UpdateBadgeInstance(
        Guid teamId,
        Guid eventId,
        Guid badgeTypeId,
        Guid badgeInstanceId,
        UpdateBadgeInstanceHttpRequest request,
        ICommandHandler<UpdateBadgeInstanceCommand> handler,
        [FromKeyedServices(BadgesModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            request.ToCommand(eventId, teamId, badgeTypeId, badgeInstanceId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
