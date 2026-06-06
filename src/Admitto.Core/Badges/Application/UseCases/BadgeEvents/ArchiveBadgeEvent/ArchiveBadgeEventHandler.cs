using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.ArchiveBadgeEvent;

internal sealed class ArchiveBadgeEventHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<ArchiveBadgeEventCommand>
{
    public async ValueTask HandleAsync(ArchiveBadgeEventCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);

        var badgesEvent = await writeStore.BadgeEvents.FindAsync([eventId], cancellationToken);

        badgesEvent?.MarkArchived();
    }
}
