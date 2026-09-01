using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a CouponInvitation email when a coupon is created for an attendee.
/// </summary>
internal sealed class CouponCreatedIntegrationEventHandler(
    ITransactionalEmailComposer composer,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : IIntegrationEventHandler<CouponCreatedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        CouponCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var rendered = await composer.ComposeAsync(new CouponInvitationIntent(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            integrationEvent.CouponCode), cancellationToken);
        await TransactionalEmailDeliveryPreparation.PrepareAsync(
            prepareDeliveryHandler,
            new TransactionalEmailDelivery(
                integrationEvent.TeamId,
                integrationEvent.TicketedEventId,
                integrationEvent.RecipientEmail,
                integrationEvent.RecipientEmail,
                $"coupon-created:{integrationEvent.CouponCode}"),
            rendered,
            cancellationToken);
    }
}
