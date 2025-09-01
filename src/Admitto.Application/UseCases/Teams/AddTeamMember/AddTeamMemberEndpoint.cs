using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;

/// <summary>
/// Add a new member to an organizing team.
/// </summary>
public static class AddTeamMemberEndpoint
{
    public static RouteGroupBuilder MapAddTeamMember(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{teamSlug}/members", AddTeamMember)
            .WithName(nameof(AddTeamMember))
            .RequireAuthorization(policy => policy.RequireCanUpdateTeam());
        
        return group;
    }
    
    private static async ValueTask<Created> AddTeamMember(
        string teamSlug,
        AddTeamMemberRequest request,
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
        
        team.AddMember(request.Email, request.Role);

        return TypedResults.Created();
    }
}
