using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeCouponEmail;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a CouponInvitation email when a coupon is created for an attendee.
/// </summary>
internal sealed class CouponCreatedIntegrationEventHandler(
    ICouponEmailComposer composer)
    : IIntegrationEventHandler<CouponCreatedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        CouponCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await composer.ComposeAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            new CouponInvitationIntent(integrationEvent.CouponCode),
            new CouponEmailDelivery(
                integrationEvent.RecipientEmail,
                integrationEvent.RecipientEmail,
                $"coupon-created:{integrationEvent.CouponCode}"),
            cancellationToken);
    }
}
