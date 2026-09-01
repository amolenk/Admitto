using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

internal sealed class TicketConfirmationResendRequestedIntegrationEventHandler(
    ITicketConfirmationEmailComposer composer)
    : IIntegrationEventHandler<TicketConfirmationResendRequestedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketConfirmationResendRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var registrationId = RegistrationId.From(integrationEvent.RegistrationId);
        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        var idempotencyKey = $"ticket-confirmation-resend:{integrationEvent.RegistrationId}:{integrationEvent.ResendRequestId}";
        await composer.ComposeAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            new TicketConfirmationIntent(
                registrationId,
                integrationEvent.FirstName,
                integrationEvent.TicketNames),
            new TicketConfirmationDelivery(integrationEvent.RecipientEmail, fullName, idempotencyKey),
            cancellationToken);
    }
}
