using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.ArchiveBadgeEvent.EventHandlers;

internal sealed class TicketedEventArchivedIntegrationEventHandler(
    ICommandHandler<ArchiveBadgeEventCommand> handler)
    : IIntegrationEventHandler<TicketedEventArchivedIntegrationEvent>
{
    public ValueTask HandleAsync(
        TicketedEventArchivedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var command = new ArchiveBadgeEventCommand(integrationEvent.TicketedEventId);

        return handler.HandleAsync(command, cancellationToken);
    }
}
