using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Teams.UpdateTeam;

/// <summary>
/// Updates the details of an existing team.
/// </summary>
public static class CreateTeamEndpoint
{
    public static RouteGroupBuilder MapUpdateTeam(this RouteGroupBuilder group)
    {
        group
            .MapPatch("/{teamSlug}", UpdateTeam)
            .WithName(nameof(UpdateTeam))
            .RequireAuthorization(policy => policy.RequireCanUpdateTeam());
        
        return group;
    }

    private static async ValueTask<Ok> UpdateTeam(
        string teamSlug,
        UpdateTeamRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);

        var team = await context.Teams
            .FirstOrDefaultAsync(te => te.Id == teamId, cancellationToken);
        if (team is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Team.NotFound);
        }

        team.UpdateDetails(
            request.Name,
            request.Email,
            request.EmailServiceConnectionString);

        return TypedResults.Ok();
    }
}
