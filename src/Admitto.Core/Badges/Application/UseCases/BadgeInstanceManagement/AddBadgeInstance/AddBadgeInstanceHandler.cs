using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.AddBadgeInstance;

internal sealed class AddBadgeInstanceHandler(IBadgesWriteStore writeStore)
    : ICommandHandler<AddBadgeInstanceCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(AddBadgeInstanceCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);

        var badgesEvent = await writeStore.BadgesEvents.GetUntrackedAsync(
            be => be.Id == eventId,
            cancellationToken);

        badgesEvent.EnsureEventActive();

        var badgeTypeId = BadgeTypeId.From(command.BadgeTypeId);

        var badgeType = await writeStore.BadgeTypes.GetUntrackedAsync(
            bt => bt.Id == badgeTypeId && bt.EventId == eventId,
            cancellationToken);

        if (badgeType.Kind != BadgeKind.Standalone)
            throw new BusinessRuleViolationException(Errors.NotStandaloneBadgeType);

        var id = BadgeInstanceId.New();
        var displayName = BadgeInstanceDisplayName.From(command.DisplayName);
        var notes = BadgeInstanceNotes.From(command.Notes);

        var instance = BadgeInstance.Create(id, badgeTypeId, displayName, notes);
        writeStore.BadgeInstances.Add(instance);

        return id.Value;
    }

    internal static class Errors
    {
        public static readonly Error NotStandaloneBadgeType = new(
            "badge_instance.not_standalone_badge_type",
            "Badge instances can only be added to standalone badge types.",
            Type: ErrorType.Validation);
    }
}
