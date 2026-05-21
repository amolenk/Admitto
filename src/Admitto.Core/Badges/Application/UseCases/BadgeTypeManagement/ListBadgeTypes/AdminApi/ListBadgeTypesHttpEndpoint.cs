using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.ListBadgeTypes.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.ListBadgeTypes.AdminApi;

public static class ListBadgeTypesHttpEndpoint
{
    public static RouteGroupBuilder MapListBadgeTypes(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", ListBadgeTypes)
            .WithName(nameof(ListBadgeTypes))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<BadgeTypeListItemDto>>> ListBadgeTypes(
        Guid teamId,
        Guid eventId,
        IQueryHandler<ListBadgeTypesQuery, IReadOnlyList<BadgeTypeListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListBadgeTypesQuery(eventId), cancellationToken);

        return TypedResults.Ok(result);
    }
}
