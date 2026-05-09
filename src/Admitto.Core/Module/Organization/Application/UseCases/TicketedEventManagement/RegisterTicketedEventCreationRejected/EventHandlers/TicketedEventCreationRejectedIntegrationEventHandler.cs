using Amolenk.Admitto.Core.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected.EventHandlers;

internal sealed class TicketedEventCreationRejectedIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<TicketedEventCreationRejected>
{
    public ValueTask HandleAsync(
        TicketedEventCreationRejected integrationEvent,
        CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventCreationRejectedCommand(
            integrationEvent.TeamId,
            integrationEvent.CreationRequestId,
            integrationEvent.Reason)
        {
            CommandId = DeterministicGuid.Create($"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCreationRejected)}")
        };


        return mediator.SendAsync(command, cancellationToken);
    }
}
