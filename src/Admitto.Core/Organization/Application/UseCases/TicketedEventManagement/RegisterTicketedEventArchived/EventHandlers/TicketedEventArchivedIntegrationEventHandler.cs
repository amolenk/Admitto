using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventArchived.EventHandlers;

internal sealed class TicketedEventArchivedIntegrationEventHandler(RegisterTicketedEventArchivedHandler handler)
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
