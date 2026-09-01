using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;

internal interface ITicketConfirmationEmailComposer
{
    ValueTask ComposeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        TicketConfirmationIntent intent,
        TicketConfirmationDelivery delivery,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Attendee and ticket facts used by the built-in ticket confirmation template.
/// Event facts, branding, and links are loaded into an immutable scope by the composer.
/// </summary>
internal sealed record TicketConfirmationIntent(
    RegistrationId RegistrationId,
    string FirstName,
    IReadOnlyList<string> TicketTypes);

internal sealed record TicketConfirmationDelivery(
    string RecipientAddress,
    string RecipientName,
    string IdempotencyKey);

internal sealed class TicketConfirmationEmailComposer(
    IEmailWriteStore writeStore,
    IEventEmailRenderingContextProvider eventContextProvider,
    IEmailPreparationService preparationService,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : ITicketConfirmationEmailComposer
{
    public async ValueTask ComposeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        TicketConfirmationIntent intent,
        TicketConfirmationDelivery delivery,
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
        // loading projections so a redelivery remains a no-op even after projection loss.
        if (existing?.IsTerminal == true)
            return;

        // The immutable scope is deliberately local to this use case. Missing or
        // incomplete event context therefore fails before rendering or claim preparation.
        var context = await eventContextProvider.GetScopeAsync(
            teamId,
            ticketedEventId,
            cancellationToken);
        var links = context.GetRegistrationLinks(intent.RegistrationId);
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["first_name"] = intent.FirstName,
            ["team_name"] = context.TeamName,
            ["event_name"] = context.EventName,
            ["public_event_link"] = links.PublicEventLink,
            ["qrcode_link"] = links.QRCodeLink,
            ["edit_registration_link"] = links.EditRegistrationLink,
            ["ticket_types"] = intent.TicketTypes
        };

        var rendered = await preparationService.PrepareAsync(
            BuiltInEmailTemplateNames.TicketConfirmation,
            teamId,
            ticketedEventId,
            parameters,
            context.AccentColor,
            cancellationToken);

        // PrepareEmailDelivery remains the single claim/outbox boundary. Its own
        // terminal check protects the concurrent race between this pre-check and claim.
        await prepareDeliveryHandler.HandleAsync(
            new PrepareEmailDeliveryCommand(
                teamId.Value,
                ticketedEventId.Value,
                delivery.RecipientAddress,
                delivery.RecipientName,
                BuiltInEmailTemplateNames.TicketConfirmation,
                delivery.IdempotencyKey,
                rendered.Subject,
                rendered.TextBody,
                rendered.HtmlBody,
                intent.RegistrationId.Value),
            cancellationToken);
    }
}
