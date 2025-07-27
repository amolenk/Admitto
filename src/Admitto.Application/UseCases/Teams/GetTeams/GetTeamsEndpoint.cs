using System.Security.Claims;
using Amolenk.Admitto.Application.Common.Authorization;

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
        IAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var authorizedTeams = (
                await authorizationService.GetTeamsAsync(userId.Value, cancellationToken))
            .ToList();

        if (authorizedTeams.Count == 0)
        {
            return TypedResults.Ok(new GetTeamsResponse([]));
        }

        var teams = await context.Teams
            .Where(t => authorizedTeams.Contains(t.Slug))
            .ToListAsync(cancellationToken);

        var response = new GetTeamsResponse(
            teams
                .Select(t => new TeamDto(t.Slug, t.Name, t.Email))
                .ToArray());

        return TypedResults.Ok(response);
    }
}