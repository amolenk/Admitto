using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail.EventHandlers;

/// <summary>
/// Handles <see cref="RegistrationCancelledIntegrationEvent"/> by dispatching a
/// <see cref="SendEmailCommand"/> with the appropriate cancellation template.
/// </summary>
/// <remarks>
/// Template routing: AttendeeRequest → cancellation; VisaLetterDenied → visa-letter-denied.
/// TicketTypesRemoved is a no-op (handled by a future change).
/// Idempotency key: <c>registration-cancelled:{registrationId}</c>.
/// </remarks>
internal sealed class RegistrationCancelledIntegrationEventHandler(
    IEmailWriteStore writeStore,
    IRegistrationsFacade registrationsFacade,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<RegistrationCancelledIntegrationEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var emailType = ResolveEmailType(integrationEvent.Reason);
        if (emailType is null)
            return;

        var idempotencyKey = $"registration-cancelled:{integrationEvent.RegistrationId}";

        var alreadyHandled = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);

        if (alreadyHandled)
            return;

        var eventContext = await registrationsFacade.GetTicketedEventEmailContextAsync(
            integrationEvent.TicketedEventId,
            integrationEvent.RegistrationId,
            cancellationToken);

        var firstName = eventContext.FirstName ?? string.Empty;
        var lastName = eventContext.LastName ?? string.Empty;

        var command = new SendEmailCommand(
            TeamId: integrationEvent.TeamId,
            TicketedEventId: integrationEvent.TicketedEventId,
            RegistrationId: integrationEvent.RegistrationId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: $"{firstName} {lastName}".Trim(),
            EmailType: emailType,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                FirstName = firstName,
                LastName = lastName,
                EventName = eventContext.Name,
                EventWebsite = eventContext.WebsiteUrl,
                RegisterLink = eventContext.RegisterLink,
                QRCodeLink = eventContext.QRCodeLink
            });

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }

    private static string? ResolveEmailType(string reason) => reason switch
    {
        "AttendeeRequest" => BuiltInEmailTemplateNames.Cancellation,
        "VisaLetterDenied" => BuiltInEmailTemplateNames.VisaLetterDenied,
        "ReconfirmAutoCancel" => BuiltInEmailTemplateNames.ReconfirmCancelled,
        _ => null
    };
}
