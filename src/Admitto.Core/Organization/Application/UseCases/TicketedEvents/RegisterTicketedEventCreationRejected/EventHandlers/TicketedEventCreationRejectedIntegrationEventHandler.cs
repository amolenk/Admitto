using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreationRejected.EventHandlers;

internal sealed class TicketedEventCreationRejectedIntegrationEventHandler(
    ICommandHandler<RegisterTicketedEventCreationRejectedCommand> handler,
    [FromKeyedServices(OrganizationModule.Key)] IInbox inbox)
    : IIntegrationEventHandler<TicketedEventCreationRejectedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventCreationRejectedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!await inbox.TryMarkAsProcessedByAsync<TicketedEventCreationRejectedIntegrationEventHandler>(
                integrationEvent,
                cancellationToken))
        {
            return;
        }

        var command = new RegisterTicketedEventCreationRejectedCommand(
            integrationEvent.TeamId,
            integrationEvent.CreationRequestId,
            integrationEvent.Reason)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCreationRejectedIntegrationEvent)}")
        };

        await handler.HandleAsync(command, cancellationToken);
    }
}
