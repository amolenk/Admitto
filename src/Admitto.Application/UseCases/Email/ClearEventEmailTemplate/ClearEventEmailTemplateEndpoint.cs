using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.ClearEventEmailTemplate;

public static class ClearEventEmailTemplateEndpoint
{
    public static RouteGroupBuilder MapClearEventEmailTemplate(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/teams/{teamSlug}/events/{eventSlug}/email/templates/{emailType}", ClearEventEmailTemplate)
            .WithName(nameof(ClearEventEmailTemplate))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> ClearEventEmailTemplate(
        string teamSlug,
        string eventSlug,
        EmailType emailType,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var ids = await context.GetTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        await context.EmailTemplates
            .Where(t => t.TeamId == ids.TeamId && t.TicketedEventId == ids.TicketedEventId
                && t.Type == emailType)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.Ok();
    }
}