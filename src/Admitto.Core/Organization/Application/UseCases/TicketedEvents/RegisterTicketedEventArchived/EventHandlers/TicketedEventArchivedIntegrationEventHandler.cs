using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived.EventHandlers;

internal sealed class TicketedEventArchivedIntegrationEventHandler(ICommandHandler<RegisterTicketedEventArchivedCommand> handler)
    : IIntegrationEventHandler<TicketedEventArchivedIntegrationEvent>
{
    public ValueTask HandleAsync(TicketedEventArchivedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventArchivedCommand(
            integrationEvent.TeamId,
            integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventArchivedIntegrationEvent)}")
        };

        return handler.HandleAsync(command, cancellationToken);
    }
}
