using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.Email.GetEventEmailTemplates;

public static class GetEventEmailTemplatesEndpoint
{
    public static RouteGroupBuilder MapGetEventEmailTemplates(this RouteGroupBuilder group)
    {
        group
            .MapGet("/teams/{teamSlug}/events/{eventSlug}/email/templates", GetEventEmailTemplates)
            .WithName(nameof(GetEventEmailTemplates))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetEventEmailTemplatesResponse>> GetEventEmailTemplates(
        string teamSlug,
        string eventSlug,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) = await context.GetTicketedEventIdsAsync(
            teamSlug,
            eventSlug,
            cancellationToken: cancellationToken);

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