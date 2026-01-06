using System.Security.Claims;
using Amolenk.Admitto.Application.Common.Authentication;
using Amolenk.Admitto.Application.Common.Persistence;

namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

/// <summary>
/// Get all existing teams.
/// </summary>
public static class GetTeamsEndpoint
{
    public static RouteGroupBuilder MapGetTeams(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTeams)
            .WithName(nameof(GetTeams));

        return group;
    }

    private static async ValueTask<Results<Ok<GetTeamsResponse>, UnauthorizedHttpResult>> GetTeams(
        IApplicationContext context,
        ClaimsPrincipal principal,
        IAdministratorRoleService administratorRoleService,
        ITeamMemberRoleService teamMemberRoleService,
        CancellationToken cancellationToken)
    {
        var teamQuery = context.Teams.AsQueryable();

        // Administrators can see all teams, other users only the teams they are members of.
        var userId = principal.GetUserId();
        if (!administratorRoleService.IsAdministrator(userId))
        {
            var authorizedTeamIds = (
                    await teamMemberRoleService.GetTeamsAsync(userId, cancellationToken))
                .ToList();

            teamQuery = teamQuery.Where(t => authorizedTeamIds.Contains(t.Id));
        }

        var teams = await teamQuery
            .Select(t => new TeamDto(
                t.Slug,
                t.Name,
                t.Email))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(new GetTeamsResponse(teams));
    }
}