using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCancelled.EventHandlers;

internal sealed class TicketedEventCancelledIntegrationEventHandler(RegisterTicketedEventCancelledHandler handler)
    : IIntegrationEventHandler<TicketedEventCancelledIntegrationEvent>
{
    public ValueTask HandleAsync(TicketedEventCancelledIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventCancelledCommand(
            integrationEvent.TeamId,
            integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCancelledIntegrationEvent)}")
        };

        return handler.HandleAsync(command, cancellationToken);
    }
}
