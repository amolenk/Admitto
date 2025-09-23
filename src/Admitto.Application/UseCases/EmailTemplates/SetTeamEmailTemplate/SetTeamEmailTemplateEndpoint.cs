using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.SetTeamEmailTemplate;

/// <summary>
/// Represents the endpoint for configuring a team email template.
/// </summary>
public static class SetTeamEmailTemplateEndpoint
{
    public static RouteGroupBuilder MapSetTeamEmailTemplate(this RouteGroupBuilder group)
    {
        group
            .MapPut("/email-templates/{emailType}", SetTeamEmailTemplate)
            .WithName(nameof(SetTeamEmailTemplate))
            .RequireAuthorization(policy => policy.RequireCanUpdateTeam());

        return group;
    }

    private static async ValueTask<Ok> SetTeamEmailTemplate(
        string teamSlug,
        string emailType,
        SetTeamEmailTemplateRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);

        var emailTemplate = EmailTemplate.Create(
            emailType,
            request.Subject,
            request.TextBody,
            request.HtmlBody,
            teamId);
        
        var existingTemplate = await context.EmailTemplates
            .FirstOrDefaultAsync(
                et => et.TeamId == teamId && et.Type == emailType,
                cancellationToken: cancellationToken);

        if (existingTemplate is not null)
        {
            context.EmailTemplates.Remove(existingTemplate);
        }
        
        context.EmailTemplates.Add(emailTemplate);
        
        return TypedResults.Ok();
    }
}
