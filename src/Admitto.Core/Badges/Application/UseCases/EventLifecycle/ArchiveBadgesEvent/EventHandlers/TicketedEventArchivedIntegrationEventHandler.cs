using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.ArchiveBadgesEvent.EventHandlers;

internal sealed class TicketedEventArchivedIntegrationEventHandler(
    ICommandHandler<ArchiveBadgesEventCommand> handler)
    : IIntegrationEventHandler<TicketedEventArchivedIntegrationEvent>
{
    public ValueTask HandleAsync(
        TicketedEventArchivedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var command = new ArchiveBadgesEventCommand(integrationEvent.TicketedEventId);

        return handler.HandleAsync(command, cancellationToken);
    }
}
