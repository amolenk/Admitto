using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.SendReconfirmBulkEmail;

/// <summary>
/// Represents an endpoint to send a reconfirm bulk email.
/// </summary>
public static class SendReconfirmBulkEmailEndpoint
{
    public static RouteGroupBuilder MapSendReconfirmBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/reconfirm", SendReconfirmBulkEmail)
            .WithName(nameof(SendReconfirmBulkEmail))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Accepted> SendReconfirmBulkEmail(
        string teamSlug,
        string eventSlug,
        SendReconfirmBulkEmailRequest request,
        ISlugResolver slugResolver,
        ICommandSender commandSender,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var sendReconfirmBulkEmailCommand = new SendReconfirmBulkEmailCommand(
            teamId,
            eventId,
            request.InitialDelayAfterRegistration,
            request.ReminderInterval);

        commandSender.Enqueue(sendReconfirmBulkEmailCommand);
        
        return TypedResults.Accepted((string?)null);
    }
}