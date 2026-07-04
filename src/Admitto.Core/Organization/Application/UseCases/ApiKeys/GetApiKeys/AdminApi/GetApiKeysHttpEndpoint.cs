using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.GetApiKeys.AdminApi;

public static class GetApiKeysHttpEndpoint
{
    public static RouteGroupBuilder MapGetApiKeys(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetApiKeys)
            .WithName(nameof(GetApiKeys))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<ApiKeyListItemDto>>> GetApiKeys(
        Guid teamId,
        IQueryHandler<GetApiKeysQuery, IReadOnlyList<ApiKeyListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        var keys = await handler.HandleAsync(new GetApiKeysQuery(teamId), cancellationToken);

        return TypedResults.Ok(keys);
    }
}
