using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.ListBadgeInstances.AdminApi;

public static class ListBadgeInstancesHttpEndpoint
{
    public static RouteGroupBuilder MapListBadgeInstances(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{badgeTypeId:guid}/instances", ListBadgeInstances)
            .WithName(nameof(ListBadgeInstances))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<BadgeInstanceListItemDto>>> ListBadgeInstances(
        Guid teamId,
        Guid eventId,
        Guid badgeTypeId,
        IQueryHandler<ListBadgeInstancesQuery, IReadOnlyList<BadgeInstanceListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListBadgeInstancesQuery(eventId, badgeTypeId),
            cancellationToken);

        return TypedResults.Ok(result);
    }
}
