using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.SendReconfirmEmail;

/// <summary>
/// Represents an endpoint that can send a single reconfirm email message.
/// </summary>
public static class SendReconfirmEmailEndpoint
{
    public static RouteGroupBuilder MapSendReconfirmEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/reconfirm", SendReconfirmEmail)
            .WithName(nameof(SendReconfirmEmail))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Accepted> SendReconfirmEmail(
        string teamSlug,
        string eventSlug,
        SendReconfirmEmailRequest request,
        ISlugResolver slugResolver,
        ICommandSender commandSender,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var command = new SendReconfirmEmailCommand(
            teamId,
            eventId,
            request.AttendeeId);
        
        commandSender.Enqueue(command);

        return TypedResults.Accepted((string?)null);
    }
}