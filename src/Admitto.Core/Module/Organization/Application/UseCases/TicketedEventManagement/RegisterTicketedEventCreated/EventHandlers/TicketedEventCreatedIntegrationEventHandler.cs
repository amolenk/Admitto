using Amolenk.Admitto.Core.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated.EventHandlers;

internal sealed class TicketedEventCreatedIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<TicketedEventCreated>
{
    public ValueTask HandleAsync(TicketedEventCreated integrationEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventCreatedCommand(
            integrationEvent.TeamId,
            integrationEvent.CreationRequestId,
            integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCreated)}")
        };

        return mediator.SendAsync(command, cancellationToken);
    }
}
