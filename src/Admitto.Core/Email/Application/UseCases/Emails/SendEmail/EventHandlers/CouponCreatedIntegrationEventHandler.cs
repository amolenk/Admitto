using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a CouponInvitation email when a coupon is created for an attendee.
/// </summary>
internal sealed class CouponCreatedIntegrationEventHandler(
    IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> eventContextQuery,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<CouponCreatedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        CouponCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"coupon-created:{integrationEvent.CouponCode}";
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
                eventContext.TeamAccentColor
            },
            RegistrationId: null);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
