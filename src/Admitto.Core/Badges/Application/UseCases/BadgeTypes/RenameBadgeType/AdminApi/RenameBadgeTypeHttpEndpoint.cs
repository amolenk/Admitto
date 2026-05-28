using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.RenameBadgeType.AdminApi;

public static class RenameBadgeTypeHttpEndpoint
{
    public static RouteGroupBuilder MapRenameBadgeType(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{badgeTypeId:guid}", RenameBadgeType)
            .WithName(nameof(RenameBadgeType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> RenameBadgeType(
        Guid teamId,
        Guid eventId,
        Guid badgeTypeId,
        RenameBadgeTypeHttpRequest request,
        ICommandHandler<RenameBadgeTypeCommand> handler,
        [FromKeyedServices(BadgesModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request.ToCommand(eventId, teamId, badgeTypeId), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
