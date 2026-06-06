using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived.EventHandlers;

internal sealed class TicketedEventArchivedIntegrationEventHandler(
    ICommandHandler<RegisterTicketedEventArchivedCommand> handler,
    [FromKeyedServices(OrganizationModule.Key)] IInbox inbox)
    : IIntegrationEventHandler<TicketedEventArchivedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventArchivedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!await inbox.TryMarkAsProcessedByAsync<TicketedEventArchivedIntegrationEventHandler>(
                integrationEvent,
                cancellationToken))
        {
            return;
        }

        var command = new RegisterTicketedEventArchivedCommand(
            integrationEvent.TeamId,
            integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventArchivedIntegrationEvent)}")
        };

        await handler.HandleAsync(command, cancellationToken);
    }
}
