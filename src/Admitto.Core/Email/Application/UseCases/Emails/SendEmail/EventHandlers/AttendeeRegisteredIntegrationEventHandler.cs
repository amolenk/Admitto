using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a TicketConfirmation email when an attendee has registered.
/// </summary>
internal sealed class AttendeeRegisteredIntegrationEventHandler(
    IRegistrationsFacade registrationsFacade,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<AttendeeRegisteredIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"attendee-registered:{integrationEvent.RegistrationId}:{integrationEvent.RegisteredAt:O}";

        var eventContext = await registrationsFacade.GetEventRegistrationSnapshotAsync(
            integrationEvent.TeamId,
            integrationEvent.TicketedEventId,
            integrationEvent.RegistrationId,
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
                EventName = eventContext.Name,
                EventWebsite = eventContext.WebsiteUrl,
                eventContext.QRCodeLink,
                TicketTypes = ticketTypeNames
            },
            RegistrationId: integrationEvent.RegistrationId);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
