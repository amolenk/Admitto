namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.GetTeamEmailTemplates;

/// <summary>
/// Represents the endpoint for retrieving email templates for a specific team.
/// </summary>
public static class GetTeamEmailTemplatesEndpoint
{
    public static RouteGroupBuilder MapGetTeamEmailTemplates(this RouteGroupBuilder group)
    {
        group
            .MapGet("/teams/{teamSlug}/email-templates", GetTeamEmailTemplates)
            .WithName(nameof(GetTeamEmailTemplates))
            .RequireAuthorization(policy => policy.RequireCanViewTeam());

        return group;
    }

    private static async ValueTask<Ok<GetTeamEmailTemplatesResponse>> GetTeamEmailTemplates(
        string teamSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);

        var emailTemplates = await context.EmailTemplates
            .Where(t => t.TeamId == teamId && t.TicketedEventId == null)
            .ToListAsync(cancellationToken);
        
        var response = new GetTeamEmailTemplatesResponse(
            emailTemplates
                .Select(t => new TeamEmailTemplateDto(t.Type))
                .ToArray());
        
        return TypedResults.Ok(response);
    }
}