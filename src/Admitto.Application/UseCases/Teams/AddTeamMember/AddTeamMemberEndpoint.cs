using Amolenk.Admitto.Application.Common.Authorization;

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
        var teamId = await slugResolver.GetTeamIdAsync(teamSlug, cancellationToken);
        var team = await context.Teams.GetEntityAsync(teamId, cancellationToken: cancellationToken);
        
        team.AddMember(request.Email, request.Role);

        return TypedResults.Created();
    }
}
