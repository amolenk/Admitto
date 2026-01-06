using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.TestEmail;

/// <summary>
/// Represents an endpoint that can send a single test email message.
/// Test emails are used to verify the email configuration and ensure that the email system is functioning correctly.
/// They use the same email composition and dispatching logic as regular emails, but are sent to a specified recipient
/// without being associated with any specific data entity.
/// </summary>
public static class TestEmailEndpoint
{
    public static RouteGroupBuilder MapTestEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{emailType}/test", TestEmail)
            .WithName(nameof(TestEmail))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Accepted> TestEmail(
        string teamSlug,
        string eventSlug,
        string emailType,
        TestEmailRequest request,
        ISlugResolver slugResolver,
        IMessageOutbox messageOutbox,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var command = new TestEmailCommand(
            teamId,
            eventId,
            request.Recipient,
            emailType,
            request.AdditionalDetails ?? [],
            request.Tickets ?? []);
        
        messageOutbox.Enqueue(command);

        return TypedResults.Accepted((string?)null);
    }
}