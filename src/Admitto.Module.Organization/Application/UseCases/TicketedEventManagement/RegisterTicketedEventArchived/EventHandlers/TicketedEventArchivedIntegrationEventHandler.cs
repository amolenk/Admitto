using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventArchived.EventHandlers;

internal sealed class TicketedEventArchivedIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<TicketedEventArchived>
{
    public ValueTask HandleAsync(TicketedEventArchived integrationEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventArchivedCommand(
            integrationEvent.TeamId,
            integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventArchived)}")
        };

        return mediator.SendAsync(command, cancellationToken);
    }
}
