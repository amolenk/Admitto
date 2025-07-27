using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Cryptography;

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
            .RequireAuthorization(policy => policy.RequireCanViewTeam());

        return group;
    }

    private static async ValueTask<Ok<GetTeamResponse>> GetTeam(
        string teamSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        ITeamConfigEncryptionService encryptionService,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.GetTeamIdAsync(teamSlug, cancellationToken);
        
        var team = await context.Teams.GetEntityAsync(teamId, true, cancellationToken);

        var response = new GetTeamResponse(
            team.Slug,
            team.Name,
            team.Email,
            encryptionService.Decrypt(team.EmailServiceConnectionString));

        return TypedResults.Ok(response);
    }
}