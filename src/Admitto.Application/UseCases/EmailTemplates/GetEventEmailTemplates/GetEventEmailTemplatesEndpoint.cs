using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.GetEventEmailTemplates;

/// <summary>
/// Represents the endpoint for retrieving email templates for a specific event.
/// </summary>
public static class GetEventEmailTemplatesEndpoint
{
    public static RouteGroupBuilder MapGetEventEmailTemplates(this RouteGroupBuilder group)
    {
        group
            .MapGet("/teams/{teamSlug}/events/{eventSlug}/email-templates", GetEventEmailTemplates)
            .WithName(nameof(GetEventEmailTemplates))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetEventEmailTemplatesResponse>> GetEventEmailTemplates(
        string teamSlug,
        string eventSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var emailTemplates = await context.EmailTemplates
            .Where(t => t.TeamId == teamId && (t.TicketedEventId == eventId || t.TicketedEventId == null))
            .ToListAsync(cancellationToken);

        var response = new GetEventEmailTemplatesResponse(
            emailTemplates
                .Select(t => new EventEmailTemplateDto(t.Type, t.TicketedEventId))
                .ToArray());

        return TypedResults.Ok(response);
    }
}