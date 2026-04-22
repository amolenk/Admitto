using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCancelled.EventHandlers;

internal sealed class TicketedEventCancelledIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<TicketedEventCancelled>
{
    public ValueTask HandleAsync(TicketedEventCancelled integrationEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventCancelledCommand(
            integrationEvent.TeamId,
            integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCancelled)}")
        };

        return mediator.SendAsync(command, cancellationToken);
    }
}
