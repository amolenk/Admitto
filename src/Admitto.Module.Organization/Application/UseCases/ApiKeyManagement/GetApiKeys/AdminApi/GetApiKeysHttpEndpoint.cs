using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.GetApiKeys.AdminApi;

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
        string teamSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, cancellationToken: cancellationToken);
        var keys = await mediator.QueryAsync<GetApiKeysQuery, IReadOnlyList<ApiKeyListItemDto>>(
            new GetApiKeysQuery(scope.TeamId), cancellationToken);

        return TypedResults.Ok(keys);
    }
}
