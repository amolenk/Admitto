using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.CreateBadgesEvent.EventHandlers;

internal sealed class TicketedEventCreatedIntegrationEventHandler(
    ICommandHandler<CreateBadgesEventCommand> handler)
    : IIntegrationEventHandler<TicketedEventCreatedIntegrationEvent>
{
    public ValueTask HandleAsync(
        TicketedEventCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var command = new CreateBadgesEventCommand(integrationEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create(
                $"{integrationEvent.IntegrationEventId}:{nameof(TicketedEventCreatedIntegrationEvent)}:badges")
        };

        return handler.HandleAsync(command, cancellationToken);
    }
}
