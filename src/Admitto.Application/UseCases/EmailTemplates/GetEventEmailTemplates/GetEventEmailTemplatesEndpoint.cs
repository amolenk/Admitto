using Amolenk.Admitto.Application.Common.Email;

namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.GetEventEmailTemplates;

/// <summary>
/// Represents the endpoint for retrieving email templates for a specific event.
/// </summary>
public static class GetEventEmailTemplatesEndpoint
{
    public static RouteGroupBuilder MapGetEventEmailTemplates(this RouteGroupBuilder group)
    {
        group
            .MapGet("/events/{eventSlug}/email-templates", GetEventEmailTemplates)
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
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var customTemplates = await context.EmailTemplates
            .Where(t => t.TeamId == teamId && (t.TicketedEventId == eventId || t.TicketedEventId == null))
            .Select(t => new EventEmailTemplateDto(t.Type, t.TicketedEventId, true))
            .ToListAsync(cancellationToken);

        var customizedTypes = customTemplates.Select(t => t.Type).ToHashSet();

        var nonCustomTemplates = WellKnownEmailType.All
            .Where(t => !customizedTypes.Contains(t))
            .Select(t => new EventEmailTemplateDto(t, null, false));

        var allTemplates = nonCustomTemplates.Concat(customTemplates)
            .OrderBy(t => t.Type)
            .ToArray();

        return TypedResults.Ok(new GetEventEmailTemplatesResponse(allTemplates));
    }
}