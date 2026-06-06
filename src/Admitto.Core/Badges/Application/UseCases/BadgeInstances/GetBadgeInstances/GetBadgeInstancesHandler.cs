using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances;

internal sealed class GetBadgeInstancesHandler(IBadgesWriteStore writeStore)
    : IQueryHandler<GetBadgeInstancesQuery, IReadOnlyList<BadgeInstanceListItemDto>>
{
    public async ValueTask<IReadOnlyList<BadgeInstanceListItemDto>> HandleAsync(
        GetBadgeInstancesQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(query.EventId);
        var badgeTypeId = BadgeTypeId.From(query.BadgeTypeId);

        var badgeType = await writeStore.BadgeTypes.GetUntrackedAsync(
            bt => bt.Id == badgeTypeId && bt.EventId == eventId,
            cancellationToken);

        if (badgeType.Kind != BadgeKind.Standalone)
            throw new BusinessRuleViolationException(Errors.NotStandaloneBadgeType);

        var instances = await writeStore.BadgeInstances
            .AsNoTracking()
            .Where(bi => bi.BadgeTypeId == badgeTypeId)
            .OrderBy(bi => bi.DisplayName)
            .ToListAsync(cancellationToken);

        return instances
            .Select(bi => new BadgeInstanceListItemDto(
                bi.Id.Value,
                bi.DisplayName.Value,
                bi.Notes.Value,
                bi.Version))
            .ToList();
    }

    internal static class Errors
    {
        public static readonly Error NotStandaloneBadgeType = new(
            "badge_instance.not_standalone_badge_type",
            "Badge instances can only be listed for standalone badge types.",
            Type: ErrorType.Validation);
    }
}
