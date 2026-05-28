using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.DeleteBadgeInstance.AdminApi;

public static class DeleteBadgeInstanceHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteBadgeInstance(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{badgeTypeId:guid}/instances/{badgeInstanceId:guid}", DeleteBadgeInstance)
            .WithName(nameof(DeleteBadgeInstance))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> DeleteBadgeInstance(
        Guid teamId,
        Guid eventId,
        Guid badgeTypeId,
        Guid badgeInstanceId,
        ICommandHandler<DeleteBadgeInstanceCommand> handler,
        [FromKeyedServices(BadgesModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new DeleteBadgeInstanceCommand(eventId, teamId, badgeTypeId, badgeInstanceId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
