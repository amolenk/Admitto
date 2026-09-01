using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a new TicketConfirmation email when an attendee's tickets have changed.
/// </summary>
internal sealed class AttendeeTicketsChangedIntegrationEventHandler(
    ITransactionalEmailComposer composer,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : IIntegrationEventHandler<AttendeeTicketsChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeTicketsChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var changedAtMs = integrationEvent.ChangedAt.ToUnixTimeMilliseconds();
        var idempotencyKey = $"tickets-changed:{integrationEvent.RegistrationId}:{changedAtMs}";

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        var intent = new TicketConfirmationIntent(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            RegistrationId.From(integrationEvent.RegistrationId),
            integrationEvent.FirstName,
            integrationEvent.NewTickets.Select(t => t.Name).ToArray());
        var rendered = await composer.ComposeAsync(intent, cancellationToken);
        await TransactionalEmailDeliveryPreparation.PrepareAsync(
            prepareDeliveryHandler,
            new TransactionalEmailDelivery(
                integrationEvent.TeamId,
                integrationEvent.TicketedEventId,
                integrationEvent.RecipientEmail,
                fullName,
                idempotencyKey,
                integrationEvent.RegistrationId),
            rendered,
            cancellationToken);
    }
}
