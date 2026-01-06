using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.SetEventEmailTemplate;

/// <summary>
/// Represents the endpoint for configuring an event email template.
/// </summary>
public static class SetEventEmailTemplateEndpoint
{
    public static RouteGroupBuilder MapSetEventEmailTemplate(this RouteGroupBuilder group)
    {
        group
            .MapPut(
                "/events/{eventSlug}/email-templates/{emailType}",
                SetEventEmailTemplate)
            .WithName(nameof(SetEventEmailTemplate))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> SetEventEmailTemplate(
        string teamSlug,
        string eventSlug,
        string emailType,
        SetEventEmailTemplateRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var emailTemplate = EmailTemplate.Create(
            emailType,
            request.Subject,
            request.TextBody,
            request.HtmlBody,
            teamId,
            eventId);

        var existingTemplate = await context.EmailTemplates
            .FirstOrDefaultAsync(
                et => et.TeamId == teamId && et.TicketedEventId == eventId && et.Type == emailType,
                cancellationToken: cancellationToken);

        if (existingTemplate is not null)
        {
            context.EmailTemplates.Remove(existingTemplate);
        }

        context.EmailTemplates.Add(emailTemplate);

        return TypedResults.Ok();
    }
}