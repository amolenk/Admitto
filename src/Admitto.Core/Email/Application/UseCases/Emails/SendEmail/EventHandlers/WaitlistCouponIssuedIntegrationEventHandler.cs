using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeCouponEmail;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Handles <see cref="WaitlistCouponIssuedIntegrationEvent"/> by sending a notification email
/// containing the coupon code and expiry to the waiting attendee.
/// Idempotency key: <c>waitlist-coupon-issued:{teamId}:{ticketedEventId}:{couponCode}</c>.
/// </summary>
internal sealed class WaitlistCouponIssuedIntegrationEventHandler(
    ICouponEmailComposer composer)
    : IIntegrationEventHandler<WaitlistCouponIssuedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        WaitlistCouponIssuedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey =
            $"waitlist-coupon-issued:{integrationEvent.TeamId}:{integrationEvent.TicketedEventId}:{integrationEvent.CouponCode}";
        await composer.ComposeAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            new WaitlistOfferIntent(
                integrationEvent.CouponCode,
                integrationEvent.TicketTypeName,
                integrationEvent.ExpiresAt),
            new CouponEmailDelivery(
                integrationEvent.RecipientEmail,
                integrationEvent.RecipientEmail,
                idempotencyKey),
            cancellationToken);
    }
}
