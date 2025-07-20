using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.ConfigureEventEmailTemplate;

public static class ConfigureEventEmailTemplateEndpoint
{
    public static RouteGroupBuilder MapConfigureEventEmailTemplate(this RouteGroupBuilder group)
    {
        group
            .MapPut(
                "/teams/{teamSlug}/events/{eventSlug}/email/templates/{emailType}",
                ConfigureEventEmailTemplate)
            .WithName(nameof(ConfigureEventEmailTemplate))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> ConfigureEventEmailTemplate(
        string teamSlug,
        string eventSlug,
        EmailType emailType,
        ConfigureEventEmailTemplateRequest request,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var ids = await context.GetTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var emailTemplate = EmailTemplate.Create(
            emailType,
            request.Subject,
            request.Body,
            ids.TeamId,
            ids.TicketedEventId);

        var existingTemplate = await context.EmailTemplates
            .FirstOrDefaultAsync(
                et => et.TeamId == ids.TeamId && et.TicketedEventId == ids.TicketedEventId &&
                      et.Type == emailType,
                cancellationToken: cancellationToken);

        if (existingTemplate is not null)
        {
            context.EmailTemplates.Remove(existingTemplate);
        }

        context.EmailTemplates.Add(emailTemplate);

        return TypedResults.Ok();
    }
}