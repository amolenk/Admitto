using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistCouponIssuedIntegrationEvent"/> by sending a notification email
/// containing the coupon code and expiry to the waiting attendee.
/// Idempotency key: <c>waitlist-coupon-issued:{teamId}:{ticketedEventId}:{couponCode}</c>.
/// </summary>
internal sealed class WaitlistCouponIssuedIntegrationEventHandler(
    IEmailWriteStore writeStore,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<WaitlistCouponIssuedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        WaitlistCouponIssuedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey =
            $"waitlist-coupon-issued:{integrationEvent.TeamId}:{integrationEvent.TicketedEventId}:{integrationEvent.CouponCode}";

        var alreadyHandled = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);

        if (alreadyHandled)
            return;

        var command = new SendEmailCommand(
            TeamId: integrationEvent.TeamId,
            TicketedEventId: integrationEvent.TicketedEventId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: integrationEvent.RecipientEmail,
            EmailType: BuiltInEmailTemplateNames.WaitlistNotification,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                integrationEvent.CouponCode,
                integrationEvent.TicketTypeName,
                ExpiresAt = integrationEvent.ExpiresAt.ToString("f"),
            },
            RegistrationId: null);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
