using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated.EventHandlers;

internal sealed class TicketedEventCreatedIntegrationEventHandler(RegisterTicketedEventCreatedHandler handler)
    : IIntegrationEventHandler<TicketedEventCreatedIntegrationEvent>
{
    public ValueTask HandleAsync(TicketedEventCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventCreatedCommand(
            integrationEvent.TeamId,
            integrationEvent.CreationRequestId,
            integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCreatedIntegrationEvent)}")
        };

        return handler.HandleAsync(command, cancellationToken);
    }
}
