using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a TicketConfirmation email when an attendee has registered.
/// </summary>
internal sealed class AttendeeRegisteredIntegrationEventHandler(
    ITicketConfirmationEmailComposer composer)
    : IIntegrationEventHandler<AttendeeRegisteredIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"attendee-registered:{integrationEvent.RegistrationId}:{integrationEvent.RegisteredAt:O}";

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        await composer.ComposeAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            new TicketConfirmationIntent(
                RegistrationId.From(integrationEvent.RegistrationId),
                integrationEvent.FirstName,
                integrationEvent.Tickets.Select(t => t.Name).ToArray()),
            new TicketConfirmationDelivery(integrationEvent.RecipientEmail, fullName, idempotencyKey),
            cancellationToken);
    }
}
