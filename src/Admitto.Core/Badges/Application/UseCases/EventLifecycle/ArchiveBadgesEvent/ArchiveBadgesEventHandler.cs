using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.ArchiveBadgesEvent;

internal sealed class ArchiveBadgesEventHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<ArchiveBadgesEventCommand>
{
    public async ValueTask HandleAsync(ArchiveBadgesEventCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);

        var badgesEvent = await writeStore.BadgesEvents.FindAsync([eventId], cancellationToken);
        if (badgesEvent is null)
            return;

        badgesEvent.MarkArchived();
    }
}
