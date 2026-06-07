using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance;

internal sealed class UpdateBadgeInstanceHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<UpdateBadgeInstanceCommand>
{
    public async ValueTask HandleAsync(UpdateBadgeInstanceCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);

        // Load BadgeEvent (untracked for guard - we don't mutate it here)
        var badgeEvent = await writeStore.BadgeEvents.GetUntrackedAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        badgeEvent.EnsureEventActive();

        var badgeInstanceId = BadgeInstanceId.From(command.BadgeInstanceId);
        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);

        // Load the instance (tracked, using its expectedVersion)
        var instance = await writeStore.BadgeInstances.GetAsync(
            bi => bi.Id == badgeInstanceId
                && bi.TeamId == teamId
                && bi.EventId == eventId
                && bi.BadgeTypeId == badgeTypeId,
            command.ExpectedVersion,
            cancellationToken);

        instance.Update(
            BadgeInstanceDisplayName.From(command.DisplayName),
            BadgeInstanceNotes.From(command.Notes));
    }
}
