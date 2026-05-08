using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail.EventHandlers;

/// <summary>
/// Handles <see cref="OtpCodeRequestedIntegrationEvent"/> by dispatching a
/// <see cref="SendEmailCommand"/> to send the OTP verification code to the attendee.
/// Idempotency key: <c>otp-requested:{otpCodeId}</c>.
/// </summary>
internal sealed class OtpCodeRequestedIntegrationEventHandler(
    IEmailWriteStore writeStore,
    IMediator mediator)
    : IIntegrationEventHandler<OtpCodeRequestedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        OtpCodeRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"otp-requested:{integrationEvent.OtpCodeId}";

        var alreadyHandled = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);

        if (alreadyHandled)
            return;

        var command = new SendEmailCommand(
            TeamId: integrationEvent.TeamId,
            TicketedEventId: integrationEvent.TicketedEventId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: integrationEvent.RecipientEmail,
            EmailType: BuiltInEmailTemplateNames.VerificationCode,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                integrationEvent.PlainCode,
                integrationEvent.EventName,
                integrationEvent.RecipientEmail
            },
            RegistrationId: null);

        await mediator.SendAsync(command, cancellationToken);
    }
}
