using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeam.AdminApi;

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
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetTeamQuery(teamId);

        var team = await mediator.QueryAsync<GetTeamQuery, TeamDto>(query, cancellationToken);

        return TypedResults.Ok(team);
    }
}