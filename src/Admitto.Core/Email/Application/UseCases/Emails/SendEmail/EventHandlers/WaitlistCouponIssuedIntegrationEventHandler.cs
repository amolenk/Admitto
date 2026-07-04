using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
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
    IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> eventContextQuery,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<WaitlistCouponIssuedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        WaitlistCouponIssuedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey =
            $"waitlist-coupon-issued:{integrationEvent.TeamId}:{integrationEvent.TicketedEventId}:{integrationEvent.CouponCode}";
        var eventContext = await eventContextQuery.HandleAsync(
            new GetEventEmailRenderingContextQuery(
                TeamId.From(integrationEvent.TeamId),
                TicketedEventId.From(integrationEvent.TicketedEventId),
                RegistrationId: null),
            cancellationToken);

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
                eventContext.TeamName,
                eventContext.EventName,
                EventWebsite = eventContext.WebsiteUrl,
                eventContext.RegisterLink,
                eventContext.TeamAccentColor
            },
            RegistrationId: null);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
