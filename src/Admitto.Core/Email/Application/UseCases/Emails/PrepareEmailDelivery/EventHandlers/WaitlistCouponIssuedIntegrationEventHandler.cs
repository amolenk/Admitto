using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistCouponIssuedIntegrationEvent"/> by sending a notification email
/// containing the coupon code and expiry to the waiting attendee.
/// Idempotency key: <c>waitlist-coupon-issued:{teamId}:{ticketedEventId}:{couponCode}</c>.
/// </summary>
internal sealed class WaitlistCouponIssuedIntegrationEventHandler(
    ITransactionalEmailComposer composer,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler,
    ILogger<WaitlistCouponIssuedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<WaitlistCouponIssuedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        WaitlistCouponIssuedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey =
            $"waitlist-coupon-issued:{integrationEvent.TeamId}:{integrationEvent.TicketedEventId}:{integrationEvent.CouponCode}";
        var reason = integrationEvent.Reason switch
        {
            nameof(WaitlistOfferReason.AutomaticPromotion) => WaitlistOfferReason.AutomaticPromotion,
            nameof(WaitlistOfferReason.VipPromotion) => WaitlistOfferReason.VipPromotion,
            nameof(WaitlistOfferReason.CapacityOpenedForEveryone) => WaitlistOfferReason.CapacityOpenedForEveryone,
            _ => LogAndDefaultToAutomaticPromotion(integrationEvent.Reason)
        };
        var rendered = await composer.ComposeAsync(new WaitlistOfferIntent(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            integrationEvent.CouponCode,
            integrationEvent.TicketTypeName,
            integrationEvent.ExpiresAt,
            reason,
            RegistrationId.From(integrationEvent.RegistrationId)),
            cancellationToken);
        await TransactionalEmailDeliveryPreparation.PrepareAsync(
            prepareDeliveryHandler,
            new TransactionalEmailDelivery(
                integrationEvent.TeamId,
                integrationEvent.TicketedEventId,
                integrationEvent.RecipientEmail,
                integrationEvent.RecipientEmail,
                idempotencyKey,
                integrationEvent.RegistrationId),
            rendered,
            cancellationToken);
    }

    private WaitlistOfferReason LogAndDefaultToAutomaticPromotion(string reason)
    {
        logger.LogWarning(
            "Unrecognized waitlist offer reason {Reason}; defaulting to {Default}",
            reason, nameof(WaitlistOfferReason.AutomaticPromotion));
        return WaitlistOfferReason.AutomaticPromotion;
    }
}
