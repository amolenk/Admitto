using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
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
            .MapPost("/teams/{teamSlug}/events/{eventSlug}/emails/{emailType}/test", TestEmail)
            .WithName(nameof(TestEmail))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> TestEmail(
        string teamSlug,
        string eventSlug,
        string emailType,
        TestEmailRequest request,
        ISlugResolver slugResolver,
        IEmailComposerRegistry emailComposerRegistry,
        IEmailDispatcher emailDispatcher,
        CancellationToken cancellationToken)
    {
        var (teamId, ticketedEventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var emailComposer = emailComposerRegistry.GetEmailComposer(emailType);

        var emailMessage = await emailComposer.ComposeTestMessageAsync(
            emailType,
            teamId,
            ticketedEventId,
            request.Recipient,
            (request.AdditionalDetails ?? [])
                .Select(ad => new AdditionalDetail(ad.Name, ad.Value))
                .ToList(),
            (request.Tickets ?? [])
                .Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity))
                .ToList(),
            cancellationToken);

        await emailDispatcher.DispatchEmailAsync(
            emailMessage,
            teamId,
            ticketedEventId,
            EmailDispatcher.TestMessageDispatchId,
            cancellationToken: cancellationToken);

        return TypedResults.Ok();
    }
}