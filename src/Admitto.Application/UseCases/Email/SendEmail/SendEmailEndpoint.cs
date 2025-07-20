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
        IDomainContext context,
        IMessageOutbox messageOutbox,
        CancellationToken cancellationToken)
    {
        var ids = await context.GetTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);
        
        var command = new SendEmailCommand(
            ids.TeamId,
            ids.TicketedEventId,
            request.EmailType,
            request.DataEntityId,
            request.RecipientEmail);
        
        messageOutbox.Enqueue(command);

        return TypedResults.Created();
    }
}