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
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
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
            nameof(WaitlistOfferReason.VipPromotion) => WaitlistOfferReason.VipPromotion,
            nameof(WaitlistOfferReason.CapacityOpenedForEveryone) => WaitlistOfferReason.CapacityOpenedForEveryone,
            _ => WaitlistOfferReason.AutomaticPromotion
        };
        var rendered = await composer.ComposeAsync(new WaitlistOfferIntent(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            integrationEvent.CouponCode,
            integrationEvent.TicketTypeName,
            integrationEvent.ExpiresAt,
            reason,
            integrationEvent.RegistrationId is { } registrationId ? RegistrationId.From(registrationId) : null),
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
}
