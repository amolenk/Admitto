using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers.AdminApi;

public static class ListTeamMembersHttpEndpoint
{
    public static RouteGroupBuilder MapListTeamMembers(this RouteGroupBuilder group)
    {
        group
            .MapGet("/members", ListTeamMembers)
            .WithName(nameof(ListTeamMembers))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<TeamMemberListItemDto>>> ListTeamMembers(
        string teamSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, cancellationToken: cancellationToken);

        var query = new GetTeamMembersQuery(scope.TeamId);

        var members = await mediator.QueryAsync<GetTeamMembersQuery, IReadOnlyList<TeamMemberListItemDto>>(
            query, cancellationToken);

        return TypedResults.Ok(members);
    }
}
