using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.DeleteBadgeInstance;

internal sealed class DeleteBadgeInstanceHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<DeleteBadgeInstanceCommand>
{
    public async ValueTask HandleAsync(DeleteBadgeInstanceCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);

        // Load BadgeEvent (untracked for guard - we don't mutate it here)
        var badgeEvent = await writeStore.BadgeEvents.GetUntrackedAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        badgeEvent.EnsureEventActive();

        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);
        var badgeInstanceId = BadgeInstanceId.From(command.BadgeInstanceId);

        // Load the instance (untracked for deletion - no version check needed for delete)
        var instance = await writeStore.BadgeInstances.GetUntrackedAsync(
            bi => bi.Id == badgeInstanceId
                && bi.TeamId == teamId
                && bi.EventId == eventId
                && bi.BadgeTypeId == badgeTypeId,
            cancellationToken: cancellationToken);

        writeStore.BadgeInstances.Remove(instance);
    }
}
