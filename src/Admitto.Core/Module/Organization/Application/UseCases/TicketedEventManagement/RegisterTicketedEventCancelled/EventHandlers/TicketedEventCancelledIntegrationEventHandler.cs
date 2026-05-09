using Amolenk.Admitto.Core.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCancelled.EventHandlers;

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
