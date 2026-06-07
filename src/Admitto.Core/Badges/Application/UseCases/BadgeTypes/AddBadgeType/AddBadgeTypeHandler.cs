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

        // Load BadgeEvent (tracked so we can mutate it)
        var badgeEvent = await writeStore.BadgeEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        var kind = Enum.Parse<BadgeKind>(command.Kind, ignoreCase: true);
        var name = BadgeTypeName.From(command.Name);
        var ticketTypeIds = TicketTypeId.ListFrom(command.TicketTypeIds);

        // Call aggregate method which enforces all business rules
        var badgeTypeId = badgeEvent.AddBadgeType(name, kind, ticketTypeIds);

        return badgeTypeId.Value;
    }
}

