namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

/// <summary>
/// Represents an endpoint that can send a single email message.
/// </summary>
public static class SendEmailEndpoint
{
    public static RouteGroupBuilder MapSendEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/teams/{teamSlug}/events/{eventSlug}/emails", SendEmail)
            .WithName(nameof(SendEmail))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Accepted> SendEmail(
        string teamSlug,
        string eventSlug,
        SendEmailRequest request,
        ISlugResolver slugResolver,
        IMessageOutbox messageOutbox,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var command = new SendEmailCommand(
            eventId,
            request.DataEntityId,
            request.EmailType,
            teamId);
        
        messageOutbox.Enqueue(command);

        return TypedResults.Accepted((string?)null);
    }
}