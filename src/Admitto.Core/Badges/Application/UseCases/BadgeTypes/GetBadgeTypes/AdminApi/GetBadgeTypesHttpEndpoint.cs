using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes.AdminApi;

public static class GetBadgeTypesHttpEndpoint
{
    public static RouteGroupBuilder MapListBadgeTypes(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetBadgeTypes)
            .WithName(nameof(GetBadgeTypes))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<GetBadgeTypesResponse>> GetBadgeTypes(
        Guid teamId,
        Guid eventId,
        IQueryHandler<GetBadgeTypesQuery, GetBadgeTypesResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetBadgeTypesQuery(eventId, teamId),
            cancellationToken);

        return TypedResults.Ok(result);
    }
}
