using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeVerificationCode;

internal interface IVerificationCodeEmailComposer
{
    ValueTask ComposeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        VerificationCodeIntent intent,
        VerificationCodeDelivery delivery,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Plain verification-code facts used by the built-in template. Event identity and
/// branding are deliberately resolved from the Email-owned rendering scope.
/// </summary>
internal sealed record VerificationCodeIntent(string PlainCode);

internal sealed record VerificationCodeDelivery(
    string RecipientAddress,
    string RecipientName,
    string IdempotencyKey);

internal sealed class VerificationCodeEmailComposer(
    IEmailWriteStore writeStore,
    IEventEmailRenderingContextProvider eventContextProvider,
    IEmailPreparationService preparationService,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : IVerificationCodeEmailComposer
{
    public async ValueTask ComposeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        VerificationCodeIntent intent,
        VerificationCodeDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        var recipient = EmailAddress.From(delivery.RecipientAddress);
        var existing = await writeStore.EmailLog.FirstOrDefaultAsync(
            log => log.TeamId == teamId
                && log.TicketedEventId == ticketedEventId
                && log.Recipient == recipient
                && log.IdempotencyKey == delivery.IdempotencyKey,
            cancellationToken);

        // Terminal claims are authoritative idempotency guards. Check them before
        // loading projections so a redelivery remains a no-op after projection loss.
        if (existing?.IsTerminal == true)
            return;

        var context = await eventContextProvider.GetScopeAsync(
            teamId,
            ticketedEventId,
            cancellationToken);

        // Keep this mapping closed: every variable in the built-in template is
        // explicit, while accent_color and font_family are added by preparation.
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["plain_code"] = intent.PlainCode,
            ["event_name"] = context.EventName,
            ["team_name"] = context.TeamName
        };

        var rendered = await preparationService.PrepareAsync(
            BuiltInEmailTemplateNames.VerificationCode,
            teamId,
            ticketedEventId,
            parameters,
            context.AccentColor,
            cancellationToken);

        // PrepareEmailDelivery is the claim/outbox boundary. Its terminal check also
        // protects the race between this pre-check and claim preparation.
        await prepareDeliveryHandler.HandleAsync(
            new PrepareEmailDeliveryCommand(
                teamId.Value,
                ticketedEventId.Value,
                delivery.RecipientAddress,
                delivery.RecipientName,
                BuiltInEmailTemplateNames.VerificationCode,
                delivery.IdempotencyKey,
                rendered.Subject,
                rendered.TextBody,
                rendered.HtmlBody,
                RegistrationId: null),
            cancellationToken);
    }
}
