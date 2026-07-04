using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.GetTeamMembers.AdminApi;

public static class GetTeamMembersHttpEndpoint
{
    public static RouteGroupBuilder MapGetTeamMembers(this RouteGroupBuilder group)
    {
        group
            .MapGet("/members", GetTeamMembers)
            .WithName(nameof(GetTeamMembers))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Owner));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<TeamMemberListItemDto>>> GetTeamMembers(
        Guid teamId,
        IQueryHandler<GetTeamMembersQuery, IReadOnlyList<TeamMemberListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTeamMembersQuery(teamId);

        var members = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(members);
    }
}
