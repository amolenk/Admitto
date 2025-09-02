using Amolenk.Admitto.Application.Common.Email;

namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.GetTeamEmailTemplates;

/// <summary>
/// Represents the endpoint for retrieving email templates for a specific team.
/// </summary>
public static class GetTeamEmailTemplatesEndpoint
{
    public static RouteGroupBuilder MapGetTeamEmailTemplates(this RouteGroupBuilder group)
    {
        group
            .MapGet("/email-templates", GetTeamEmailTemplates)
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

        var customTemplates = await context.EmailTemplates
            .Where(t => t.TeamId == teamId && t.TicketedEventId == null)
            .Select(t => new TeamEmailTemplateDto(t.Type, true))
            .ToListAsync(cancellationToken);
        
        var customizedTypes = customTemplates.Select(t => t.Type).ToHashSet();
        
        var nonCustomTemplates = WellKnownEmailType.All
            .Where(t => !customizedTypes.Contains(t))
            .Select(t => new TeamEmailTemplateDto(t, false));

        var allTemplates = nonCustomTemplates.Concat(customTemplates)
            .OrderBy(t => t.Type)
            .ToArray();

        return TypedResults.Ok(new GetTeamEmailTemplatesResponse(allTemplates));
    }
}