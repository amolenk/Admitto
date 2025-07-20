using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Validation;

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

    private static async ValueTask<Results<Ok<GetTeamResponse>, NotFound>> GetTeam(string teamSlug, 
        IDomainContext context, CancellationToken cancellationToken)
    {
        var team = await context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == teamSlug, cancellationToken);
        
        if (team is null)
        {
            return TypedResults.NotFound();
        }
        
        var response = new GetTeamResponse(
            team.Slug,
            team.Name,
            new EmailSettingsDto(
                team.EmailSettings.SenderEmail,
                team.EmailSettings.SmtpServer,
                team.EmailSettings.SmtpPort));
        
        return TypedResults.Ok(response);
    }
}
