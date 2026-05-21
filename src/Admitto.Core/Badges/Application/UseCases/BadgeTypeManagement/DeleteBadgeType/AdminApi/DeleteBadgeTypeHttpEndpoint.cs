using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.DeleteBadgeType.AdminApi;

public static class DeleteBadgeTypeHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteBadgeType(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{badgeTypeId:guid}", DeleteBadgeType)
            .WithName(nameof(DeleteBadgeType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> DeleteBadgeType(
        Guid teamId,
        Guid eventId,
        Guid badgeTypeId,
        ICommandHandler<DeleteBadgeTypeCommand> handler,
        [FromKeyedServices(BadgesModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteBadgeTypeCommand(eventId, badgeTypeId), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
