using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.AddBadgeType;

internal sealed class AddBadgeTypeHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<AddBadgeTypeCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(AddBadgeTypeCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);

        var badgeEvent = await writeStore.BadgeEvents.GetUntrackedAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        badgeEvent.EnsureEventActive();

        var kind = Enum.Parse<BadgeKind>(command.Kind, ignoreCase: true);
        var id = BadgeTypeId.New();
        var name = BadgeTypeName.From(command.Name);
        var ticketTypeIds = TicketTypeId.ListFrom(command.TicketTypeIds);

        var badgeType = BadgeType.Create(id, eventId, name, kind, ticketTypeIds);
        writeStore.BadgeTypes.Add(badgeType);

        return id.Value;
    }
}
