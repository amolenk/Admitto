using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.CreateBadgeEvent;

internal sealed class CreateBadgeEventHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<CreateBadgeEventCommand>
{
    public async ValueTask HandleAsync(CreateBadgeEventCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);

        var existing = await writeStore.BadgeEvents.FindAsync([eventId], cancellationToken);
        if (existing is not null) return;

        var badgeEvent = BadgeEvent.Create(eventId, teamId);
        writeStore.BadgeEvents.Add(badgeEvent);
    }
}
