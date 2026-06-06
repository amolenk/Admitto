using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.CreateBadgeEvent.EventHandlers;

internal sealed class TicketedEventCreatedIntegrationEventHandler(
    ICommandHandler<CreateBadgeEventCommand> handler)
    : IIntegrationEventHandler<TicketedEventCreatedIntegrationEvent>
{
    public ValueTask HandleAsync(
        TicketedEventCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var command = new CreateBadgeEventCommand(integrationEvent.TicketedEventId, integrationEvent.TeamId);

        return handler.HandleAsync(command, cancellationToken);
    }
}
