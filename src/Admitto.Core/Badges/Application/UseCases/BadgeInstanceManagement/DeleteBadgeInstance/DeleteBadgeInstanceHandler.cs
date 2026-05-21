using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.DeleteBadgeInstance;

internal sealed class DeleteBadgeInstanceHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<DeleteBadgeInstanceCommand>
{
    public async ValueTask HandleAsync(DeleteBadgeInstanceCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);

        var badgesEvent = await writeStore.BadgesEvents.GetUntrackedAsync(
            e => e.Id == eventId,
            cancellationToken);

        badgesEvent.EnsureEventActive();

        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);
        var badgeInstanceId = BadgeInstanceId.From(command.BadgeInstanceId);

        var instance = await writeStore.BadgeInstances.GetAsync(
            bi => bi.Id == badgeInstanceId && bi.BadgeTypeId == badgeTypeId,
            cancellationToken: cancellationToken);

        writeStore.BadgeInstances.Remove(instance);
    }
}
