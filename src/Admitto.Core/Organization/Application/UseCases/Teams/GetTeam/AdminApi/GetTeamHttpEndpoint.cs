using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeam.AdminApi;

public static class GetTeamHttpEndpoint
{
    public static RouteGroupBuilder MapGetTeam(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTeam)
            .WithName(nameof(GetTeam))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<TeamDto>> GetTeam(
        Guid teamId,
        IQueryHandler<GetTeamQuery, TeamDto> handler,
        CancellationToken cancellationToken)
    {
        var team = await handler.HandleAsync(new GetTeamQuery(teamId), cancellationToken);

        return TypedResults.Ok(team);
    }
}
