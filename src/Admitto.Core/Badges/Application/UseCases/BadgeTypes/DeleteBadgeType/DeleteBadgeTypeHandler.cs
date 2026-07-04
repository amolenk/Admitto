using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.DeleteBadgeType;

internal sealed class DeleteBadgeTypeHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<DeleteBadgeTypeCommand>
{
    public async ValueTask HandleAsync(DeleteBadgeTypeCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);

        // Load BadgeEvent (tracked so we can mutate it)
        var badgeEvent = await writeStore.BadgeEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);

        // Call aggregate method which enforces all business rules and returns the kind
        var kind = badgeEvent.DeleteBadgeType(badgeTypeId);

        // Cascade delete instances for standalone types
        if (kind == BadgeKind.Standalone)
        {
            var instances = await writeStore.BadgeInstances
                .Where(bi => bi.TeamId == teamId && bi.EventId == eventId && bi.BadgeTypeId == badgeTypeId)
                .ToListAsync(cancellationToken);

            writeStore.BadgeInstances.RemoveRange(instances);
        }
    }
}
