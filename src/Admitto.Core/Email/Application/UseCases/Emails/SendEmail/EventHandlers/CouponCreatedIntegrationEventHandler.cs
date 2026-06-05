using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a CouponInvitation email when a coupon is created for an attendee.
/// </summary>
internal sealed class CouponCreatedIntegrationEventHandler(
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<CouponCreatedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        CouponCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"coupon-created:{integrationEvent.CouponCode}";

        var command = new SendEmailCommand(
            TeamId: integrationEvent.TeamId,
            TicketedEventId: integrationEvent.TicketedEventId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: integrationEvent.RecipientEmail,
            EmailType: BuiltInEmailTemplateNames.CouponInvitation,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                integrationEvent.RecipientEmail,
                integrationEvent.CouponCode
            },
            RegistrationId: null);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
