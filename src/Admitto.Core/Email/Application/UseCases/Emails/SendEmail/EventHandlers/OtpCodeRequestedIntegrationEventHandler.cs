using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Handles <see cref="OtpCodeRequestedIntegrationEvent"/> by dispatching a
/// <see cref="SendEmailCommand"/> to send the OTP verification code to the attendee.
/// Idempotency key: <c>otp-requested:{otpCodeId}</c>.
/// </summary>
internal sealed class OtpCodeRequestedIntegrationEventHandler(
    IEventEmailRenderingContextProvider eventContextProvider,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<OtpCodeRequestedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        OtpCodeRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"otp-requested:{integrationEvent.OtpCodeId}";
        var eventContext = await eventContextProvider.GetContextAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            registrationId: null,
            cancellationToken: cancellationToken);

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
                eventContext.TeamName,
                eventContext.EventName,
                integrationEvent.RecipientEmail,
            },
            RegistrationId: null);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
