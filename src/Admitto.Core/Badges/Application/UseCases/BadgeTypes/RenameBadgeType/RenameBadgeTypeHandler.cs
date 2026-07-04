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

        // Load BadgeEvent (tracked so we can mutate it)
        var badgeEvent = await writeStore.BadgeEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            command.ExpectedVersion,
            cancellationToken);

        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);
        var newName = BadgeTypeName.From(command.Name);

        // Call aggregate method which enforces all business rules
        badgeEvent.RenameBadgeType(badgeTypeId, newName);
    }
}
