using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a new TicketConfirmation email when an attendee's tickets have changed.
/// </summary>
internal sealed class AttendeeTicketsChangedIntegrationEventHandler(
    IRegistrationsFacade registrationsFacade,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<AttendeeTicketsChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeTicketsChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var changedAtMs = integrationEvent.ChangedAt.ToUnixTimeMilliseconds();
        var idempotencyKey = $"tickets-changed:{integrationEvent.RegistrationId}:{changedAtMs}";

        var eventContext = await registrationsFacade.GetEventRegistrationSnapshotAsync(
            integrationEvent.TeamId,
            integrationEvent.TicketedEventId,
            integrationEvent.RegistrationId,
            cancellationToken);

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        var ticketTypeNames = integrationEvent.NewTickets.Select(t => t.Name).ToArray();

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
                QRCodeLink = eventContext.QRCodeLink,
                TicketTypes = ticketTypeNames
            },
            RegistrationId: integrationEvent.RegistrationId);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
