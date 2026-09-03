using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

internal sealed class TicketConfirmationResendRequestedIntegrationEventHandler(
    ITransactionalEmailComposer composer,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : IIntegrationEventHandler<TicketConfirmationResendRequestedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketConfirmationResendRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var registrationId = RegistrationId.From(integrationEvent.RegistrationId);
        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        var idempotencyKey = $"ticket-confirmation-resend:{integrationEvent.RegistrationId}:{integrationEvent.ResendRequestId}";
        var intent = new TicketConfirmationIntent(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            registrationId,
            integrationEvent.FirstName,
            integrationEvent.TicketNames);
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
