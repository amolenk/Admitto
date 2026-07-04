using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.AddBadgeInstance;

internal sealed class AddBadgeInstanceHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<AddBadgeInstanceCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(AddBadgeInstanceCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);

        // Load BadgeEvent (untracked for guard - we don't mutate it here)
        var badgeEvent = await writeStore.BadgeEvents.GetUntrackedAsync(
            be => be.Id == eventId && be.TeamId == teamId,
            cancellationToken);

        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);

        // Call aggregate method which enforces all business rules
        badgeEvent.EnsureCanManageInstances(badgeTypeId);

        var id = BadgeInstanceId.New();
        var displayName = BadgeInstanceDisplayName.From(command.DisplayName);
        var notes = BadgeInstanceNotes.From(command.Notes);

        var instance = BadgeInstance.Create(id, teamId, eventId, badgeTypeId, displayName, notes);
        writeStore.BadgeInstances.Add(instance);

        return id.Value;
    }
}
