using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.RenameBadgeType;

internal sealed class RenameBadgeTypeHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<RenameBadgeTypeCommand>
{
    public async ValueTask HandleAsync(RenameBadgeTypeCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);

        var badgesEvent = await writeStore.BadgesEvents.GetUntrackedAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        badgesEvent.EnsureEventActive();

        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);

        var badgeType = await writeStore.BadgeTypes.GetAsync(
            bt => bt.Id == badgeTypeId && bt.EventId == eventId,
            cancellationToken);

        badgeType.Rename(BadgeTypeName.From(command.Name));
    }
}
