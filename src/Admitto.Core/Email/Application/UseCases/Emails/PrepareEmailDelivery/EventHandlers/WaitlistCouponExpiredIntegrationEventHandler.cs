using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistCouponExpiredIntegrationEvent"/> by telling the attendee that their
/// waitlist offer lapsed unclaimed.
/// Idempotency key: <c>waitlist-coupon-expired:{teamId}:{ticketedEventId}:{couponCode}</c>.
/// </summary>
internal sealed class WaitlistCouponExpiredIntegrationEventHandler(
    ITransactionalEmailComposer composer,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : IIntegrationEventHandler<WaitlistCouponExpiredIntegrationEvent>
{
    public async ValueTask HandleAsync(
        WaitlistCouponExpiredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey =
            $"waitlist-coupon-expired:{integrationEvent.TeamId}:{integrationEvent.TicketedEventId}:{integrationEvent.CouponCode}";
        var rendered = await composer.ComposeAsync(new WaitlistOfferExpiredIntent(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            integrationEvent.TicketTypeName,
            integrationEvent.RegistrationClosed), cancellationToken);
        await TransactionalEmailDeliveryPreparation.PrepareAsync(
            prepareDeliveryHandler,
            new TransactionalEmailDelivery(
                integrationEvent.TeamId,
                integrationEvent.TicketedEventId,
                integrationEvent.RecipientEmail,
                integrationEvent.RecipientEmail,
                idempotencyKey),
            rendered,
            cancellationToken);
    }
}
