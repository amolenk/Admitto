using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a CouponInvitation email when a coupon is created for an attendee.
/// </summary>
internal sealed class CouponCreatedIntegrationEventHandler(
    IEventEmailRenderingContextProvider eventContextProvider,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<CouponCreatedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        CouponCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"coupon-created:{integrationEvent.CouponCode}";
        var eventContext = await eventContextProvider.GetContextAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            registrationId: null,
            cancellationToken: cancellationToken);

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
                integrationEvent.CouponCode,
                eventContext.TeamName,
                eventContext.EventName,
                EventWebsite = eventContext.WebsiteUrl,
                eventContext.PublicEventLink,
                eventContext.RegisterLink,
            },
            RegistrationId: null);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
