using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistJoinRequestedIntegrationEvent"/> by sending a signed
/// verification link to the attendee so they can confirm their waitlist spot.
/// Idempotency key: <c>waitlist-join-requested:{teamId}:{ticketTypeId}:{recipientEmail}</c>.
/// </summary>
internal sealed class WaitlistJoinRequestedIntegrationEventHandler(
    IEmailWriteStore writeStore,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<WaitlistJoinRequestedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        WaitlistJoinRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey =
            $"waitlist-join-requested:{integrationEvent.TeamId}:{integrationEvent.TicketTypeId}:{integrationEvent.RecipientEmail}";

        var alreadyHandled = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);

        if (alreadyHandled)
            return;

        var command = new SendEmailCommand(
            TeamId: integrationEvent.TeamId,
            TicketedEventId: integrationEvent.TicketedEventId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: integrationEvent.RecipientEmail,
            EmailType: BuiltInEmailTemplateNames.WaitlistVerification,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                integrationEvent.RecipientEmail,
                integrationEvent.VerificationToken,
                integrationEvent.TicketTypeId
            },
            RegistrationId: null);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
