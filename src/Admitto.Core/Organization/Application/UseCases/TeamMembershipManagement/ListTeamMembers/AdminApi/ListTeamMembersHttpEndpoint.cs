using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers.AdminApi;

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
        Guid teamId,
        IQueryHandler<GetTeamMembersQuery, IReadOnlyList<TeamMemberListItemDto>> handler,
        CancellationToken cancellationToken)
    {
        var members = await handler.HandleAsync(new GetTeamMembersQuery(teamId), cancellationToken);

        return TypedResults.Ok(members);
    }
}
