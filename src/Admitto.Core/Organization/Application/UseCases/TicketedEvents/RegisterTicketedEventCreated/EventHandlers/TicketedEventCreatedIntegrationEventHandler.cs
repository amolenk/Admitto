using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreated.EventHandlers;

internal sealed class TicketedEventCreatedIntegrationEventHandler(
    ICommandHandler<RegisterTicketedEventCreatedCommand> handler,
    [FromKeyedServices(OrganizationModule.Key)] IInbox inbox)
    : IIntegrationEventHandler<TicketedEventCreatedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!await inbox.TryMarkAsProcessedByAsync<TicketedEventCreatedIntegrationEventHandler>(
                integrationEvent,
                cancellationToken))
        {
            return;
        }

        var command = new RegisterTicketedEventCreatedCommand(
            integrationEvent.TeamId,
            integrationEvent.CreationRequestId,
            integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCreatedIntegrationEvent)}")
        };

        await handler.HandleAsync(command, cancellationToken);
    }
}
