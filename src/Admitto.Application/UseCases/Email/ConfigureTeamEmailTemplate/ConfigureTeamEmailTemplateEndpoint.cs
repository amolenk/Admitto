using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.ConfigureTeamEmailTemplate;

public static class ConfigureTeamEmailTemplateEndpoint
{
    public static RouteGroupBuilder MapConfigureTeamEmailTemplate(this RouteGroupBuilder group)
    {
        group
            .MapPut("/teams/{teamSlug}/email/templates/{emailType}", ConfigureTeamEmailTemplate)
            .WithName(nameof(ConfigureTeamEmailTemplate))
            .RequireAuthorization(policy => policy.RequireCanUpdateTeam());

        return group;
    }

    private static async ValueTask<Ok> ConfigureTeamEmailTemplate(
        string teamSlug,
        EmailType emailType,
        ConfigureTeamEmailTemplateRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.GetTeamIdAsync(teamSlug, cancellationToken);

        var emailTemplate = EmailTemplate.Create(
            emailType,
            request.Subject,
            request.Body,
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
