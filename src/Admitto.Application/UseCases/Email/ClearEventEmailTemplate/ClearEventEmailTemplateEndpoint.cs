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
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        await context.EmailTemplates
            .Where(t => t.TeamId == teamId && t.TicketedEventId == eventId
                                           && t.Type == emailType)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.Ok();
    }
}