using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.DeleteBadgeType;

internal sealed class DeleteBadgeTypeHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<DeleteBadgeTypeCommand>
{
    public async ValueTask HandleAsync(DeleteBadgeTypeCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);

        var badgesEvent = await writeStore.BadgesEvents.GetUntrackedAsync(
             e => e.Id == eventId,
             cancellationToken);

        badgesEvent.EnsureEventActive();

        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);

        var badgeType = await writeStore.BadgeTypes.GetAsync(
             bt => bt.Id == badgeTypeId && bt.EventId == eventId,
             cancellationToken);

        // Cascade delete instances for standalone types.
        if (badgeType.Kind == BadgeKind.Standalone)
        {
            var instances = await writeStore.BadgeInstances
                .Where(bi => bi.BadgeTypeId == badgeTypeId)
                .ToListAsync(cancellationToken);

            writeStore.BadgeInstances.RemoveRange(instances);
        }

        writeStore.BadgeTypes.Remove(badgeType);
    }
}
