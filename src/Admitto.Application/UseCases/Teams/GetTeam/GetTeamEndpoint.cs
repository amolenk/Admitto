using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeam;

/// <summary>
/// Get the details of a team.
/// </summary>
public static class GetTeamEndpoint
{
    public static RouteGroupBuilder MapGetTeam(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{teamSlug}", GetTeam)
            .WithName(nameof(GetTeam))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<GetTeamResponse>> GetTeam(
        string teamSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);
        
        var team = await context.Teams.FindAsync([teamId], cancellationToken);
        if (team is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Team.NotFound);
        }

        var response = new GetTeamResponse(
            team.Slug,
            team.Name,
            team.Email,
            team.EmailServiceConnectionString);

        return TypedResults.Ok(response);
    }
}