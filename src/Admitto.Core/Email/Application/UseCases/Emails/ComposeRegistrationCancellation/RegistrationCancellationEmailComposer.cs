using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeRegistrationCancellation;

internal interface IRegistrationCancellationEmailComposer
{
    ValueTask ComposeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        RegistrationCancellationIntent intent,
        RegistrationCancellationDelivery delivery,
        CancellationToken cancellationToken = default);
}

internal abstract record RegistrationCancellationIntent(string FirstName);

internal sealed record AttendeeRequestCancellationIntent(string FirstName)
    : RegistrationCancellationIntent(FirstName);

internal sealed record ReconfirmAutoCancellationIntent(string FirstName)
    : RegistrationCancellationIntent(FirstName);

internal sealed record VisaLetterDeniedCancellationIntent(string FirstName)
    : RegistrationCancellationIntent(FirstName);

internal sealed record RegistrationCancellationDelivery(
    string RecipientAddress,
    string RecipientName,
    string IdempotencyKey,
    RegistrationId RegistrationId);

internal sealed class RegistrationCancellationEmailComposer(
    IEmailWriteStore writeStore,
    IEventEmailRenderingContextProvider eventContextProvider,
    IEmailPreparationService preparationService,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : IRegistrationCancellationEmailComposer
{
    public async ValueTask ComposeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        RegistrationCancellationIntent intent,
        RegistrationCancellationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        var recipient = EmailAddress.From(delivery.RecipientAddress);
        var existing = await writeStore.EmailLog
            .AsNoTracking()
            .FirstOrDefaultAsync(
                log => log.TeamId == teamId
                    && log.TicketedEventId == ticketedEventId
                    && log.Recipient == recipient
                    && log.IdempotencyKey == delivery.IdempotencyKey,
                cancellationToken);

        // A terminal claim is checked before projections so a redelivery remains a
        // no-op even if the event context has since disappeared.
        if (existing?.IsTerminal == true)
            return;

        var context = await eventContextProvider.GetScopeAsync(
            teamId,
            ticketedEventId,
            cancellationToken);
        var links = context.GetRegistrationLinks(delivery.RegistrationId);

        var (emailType, parameters) = intent switch
        {
            AttendeeRequestCancellationIntent attendeeRequest =>
                (BuiltInEmailTemplateNames.Cancellation, new Dictionary<string, object?>
                {
                    ["first_name"] = attendeeRequest.FirstName,
                    ["event_name"] = context.EventName,
                    ["register_link"] = links.RegisterLink,
                    ["event_website"] = context.WebsiteUrl
                }),
            ReconfirmAutoCancellationIntent reconfirmAutoCancel =>
                (BuiltInEmailTemplateNames.ReconfirmCancelled, new Dictionary<string, object?>
                {
                    ["first_name"] = reconfirmAutoCancel.FirstName,
                    ["event_name"] = context.EventName,
                    ["register_link"] = links.RegisterLink,
                    ["event_website"] = context.WebsiteUrl
                }),
            VisaLetterDeniedCancellationIntent visaLetterDenied =>
                (BuiltInEmailTemplateNames.VisaLetterDenied, new Dictionary<string, object?>
                {
                    ["first_name"] = visaLetterDenied.FirstName,
                    ["event_name"] = context.EventName
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "Unsupported cancellation intent.")
        };

        var rendered = await preparationService.PrepareAsync(
            emailType,
            teamId,
            ticketedEventId,
            parameters,
            context.AccentColor,
            cancellationToken);

        // PrepareEmailDelivery remains the single claim/outbox boundary. Its own
        // terminal check protects the race between this pre-check and claim.
        await prepareDeliveryHandler.HandleAsync(
            new PrepareEmailDeliveryCommand(
                teamId.Value,
                ticketedEventId.Value,
                delivery.RecipientAddress,
                delivery.RecipientName,
                emailType,
                delivery.IdempotencyKey,
                rendered.Subject,
                rendered.TextBody,
                rendered.HtmlBody,
                delivery.RegistrationId.Value),
            cancellationToken);
    }
}
