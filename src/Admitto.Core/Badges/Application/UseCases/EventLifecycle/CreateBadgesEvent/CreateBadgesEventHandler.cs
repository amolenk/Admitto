using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.CreateBadgesEvent;

internal sealed class CreateBadgesEventHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<CreateBadgesEventCommand>
{
    public async ValueTask HandleAsync(CreateBadgesEventCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);

        var existing = await writeStore.BadgesEvents.FindAsync([eventId], cancellationToken);
        if (existing is not null) return;

        var badgesEvent = BadgesEvent.Create(eventId);
        writeStore.BadgesEvents.Add(badgesEvent);
    }
}
