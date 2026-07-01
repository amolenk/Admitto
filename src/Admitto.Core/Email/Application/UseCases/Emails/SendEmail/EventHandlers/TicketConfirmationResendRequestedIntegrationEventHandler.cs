using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

internal sealed class TicketConfirmationResendRequestedIntegrationEventHandler(
    IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> eventContextQuery,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<TicketConfirmationResendRequestedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketConfirmationResendRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var registrationId = RegistrationId.From(integrationEvent.RegistrationId);
        var eventContext = await eventContextQuery.HandleAsync(
            new GetEventEmailRenderingContextQuery(
                TeamId.From(integrationEvent.TeamId),
                TicketedEventId.From(integrationEvent.TicketedEventId),
                registrationId),
            cancellationToken);

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        var idempotencyKey = $"ticket-confirmation-resend:{integrationEvent.RegistrationId}:{integrationEvent.ResendRequestId}";

        var command = new SendEmailCommand(
            TeamId: integrationEvent.TeamId,
            TicketedEventId: integrationEvent.TicketedEventId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: fullName,
            EmailType: BuiltInEmailTemplateNames.TicketConfirmation,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                RecipientName = fullName,
                integrationEvent.FirstName,
                integrationEvent.LastName,
                eventContext.EventName,
                EventWebsite = eventContext.WebsiteUrl,
                eventContext.PublicEventLink,
                eventContext.QRCodeLink,
                eventContext.CancelLink,
                eventContext.TeamAccentColor,
                eventContext.ChangeTicketsLink,
                TicketTypes = integrationEvent.TicketNames
            },
            RegistrationId: integrationEvent.RegistrationId);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
