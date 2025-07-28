using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.PreviewEmail;

/// <summary>
/// Represents an endpoint that can create a preview for an email message.
/// Previews are generated based on actual email templates and data, but are not sent to any recipients.
/// </summary>
public static class PreviewEmailEndpoint
{
    public static RouteGroupBuilder MapPreviewEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/teams/{teamSlug}/events/{eventSlug}/emails/{emailType}/preview", PreviewEmail)
            .WithName(nameof(PreviewEmail))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<PreviewEmailResponse>> PreviewEmail(
        string teamSlug,
        string eventSlug,
        EmailType emailType,
        PreviewEmailRequest request,
        ISlugResolver slugResolver,
        IEmailComposerRegistry emailComposerRegistry,
        CancellationToken cancellationToken)
    {
        var (teamId, ticketedEventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var emailComposer = emailComposerRegistry.GetEmailComposer(emailType);
        
        var emailMessage = await emailComposer.ComposeMessageAsync(
            emailType,
            teamId,
            ticketedEventId,
            request.DataEntityId,
            request.AdditionalParameters,
            cancellationToken);

        var response = new PreviewEmailResponse(emailMessage.Subject, emailMessage.Body);
        
        return TypedResults.Ok(response);
    }
}