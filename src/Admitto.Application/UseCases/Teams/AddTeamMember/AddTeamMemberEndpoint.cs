using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Authentication;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

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
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Owner));

        return group;
    }

    private static async ValueTask<Created> AddTeamMember(
        string teamSlug,
        AddTeamMemberRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        IUserManagementService userManagementService,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);

        var team = await context.Teams.FindAsync([teamId], cancellationToken);
        if (team is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Team.NotFound);
        }

        var user = await userManagementService.GetUserByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            user = await userManagementService.AddUserAsync(
                request.Email,
                request.FirstName,
                request.LastName,
                cancellationToken);
        }

        team.AddMember(user.Id, request.Role);

        return TypedResults.Created();
    }
}