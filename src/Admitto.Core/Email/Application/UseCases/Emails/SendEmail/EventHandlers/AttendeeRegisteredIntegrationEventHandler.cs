using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a TicketConfirmation email when an attendee has registered.
/// </summary>
internal sealed class AttendeeRegisteredIntegrationEventHandler(
    IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> eventContextQuery,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<AttendeeRegisteredIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"attendee-registered:{integrationEvent.RegistrationId}:{integrationEvent.RegisteredAt:O}";

        var eventContext = await eventContextQuery.HandleAsync(
            new GetEventEmailRenderingContextQuery(
                TeamId.From(integrationEvent.TeamId),
                TicketedEventId.From(integrationEvent.TicketedEventId),
                RegistrationId.From(integrationEvent.RegistrationId)),
            cancellationToken);

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        var ticketTypeNames = integrationEvent.Tickets.Select(t => t.Name).ToArray();

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
                TicketTypes = ticketTypeNames
            },
            RegistrationId: integrationEvent.RegistrationId);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
