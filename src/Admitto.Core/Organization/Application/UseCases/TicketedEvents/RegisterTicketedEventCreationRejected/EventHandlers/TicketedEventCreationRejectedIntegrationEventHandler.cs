using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreationRejected.EventHandlers;

internal sealed class TicketedEventCreationRejectedIntegrationEventHandler(ICommandHandler<RegisterTicketedEventCreationRejectedCommand> handler)
    : IIntegrationEventHandler<TicketedEventCreationRejectedIntegrationEvent>
{
    public ValueTask HandleAsync(
        TicketedEventCreationRejectedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventCreationRejectedCommand(
            integrationEvent.TeamId,
            integrationEvent.CreationRequestId,
            integrationEvent.Reason)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCreationRejectedIntegrationEvent)}")
        };


        return handler.HandleAsync(command, cancellationToken);
    }
}
