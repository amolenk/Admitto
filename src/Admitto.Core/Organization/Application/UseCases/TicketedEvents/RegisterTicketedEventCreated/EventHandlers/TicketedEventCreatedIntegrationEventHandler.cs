using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreated.EventHandlers;

internal sealed class TicketedEventCreatedIntegrationEventHandler(ICommandHandler<RegisterTicketedEventCreatedCommand> handler)
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
