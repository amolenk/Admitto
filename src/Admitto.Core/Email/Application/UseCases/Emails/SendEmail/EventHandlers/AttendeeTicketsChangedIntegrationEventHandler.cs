using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a new TicketConfirmation email when an attendee's tickets have changed.
/// </summary>
internal sealed class AttendeeTicketsChangedIntegrationEventHandler(
    ITicketConfirmationEmailComposer composer)
    : IIntegrationEventHandler<AttendeeTicketsChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeTicketsChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var changedAtMs = integrationEvent.ChangedAt.ToUnixTimeMilliseconds();
        var idempotencyKey = $"tickets-changed:{integrationEvent.RegistrationId}:{changedAtMs}";

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        await composer.ComposeAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            new TicketConfirmationIntent(
                RegistrationId.From(integrationEvent.RegistrationId),
                integrationEvent.FirstName,
                integrationEvent.NewTickets.Select(t => t.Name).ToArray()),
            new TicketConfirmationDelivery(integrationEvent.RecipientEmail, fullName, idempotencyKey),
            cancellationToken);
    }
}
