using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

public static class SendEmailEndpoint
{
    public static RouteGroupBuilder MapSendEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/teams/{teamSlug}/events/{eventSlug}/email",
                SendRegistrationEmail)
            .WithName(nameof(SendRegistrationEmail))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Created> SendRegistrationEmail(
        string teamSlug,
        string eventSlug,
        SendEmailRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        IMessageOutbox messageOutbox,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);
        
        var command = new SendEmailCommand(
            teamId,
            eventId,
            request.DataEntityId,
            request.EmailType,
            request.RecipientEmail);
        
        messageOutbox.Enqueue(command);

        return TypedResults.Created();
    }
}