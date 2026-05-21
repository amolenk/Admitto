using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.ListBadgeInstances.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.ListBadgeInstances;

internal sealed class ListBadgeInstancesHandler(IBadgesWriteStore writeStore)
    : IQueryHandler<ListBadgeInstancesQuery, IReadOnlyList<BadgeInstanceListItemDto>>
{
    public async ValueTask<IReadOnlyList<BadgeInstanceListItemDto>> HandleAsync(
        ListBadgeInstancesQuery query,
        CancellationToken cancellationToken)
    {
        var badgeTypeId = BadgeTypeId.From(query.BadgeTypeId);

        var badgeType = await writeStore.BadgeTypes.GetUntrackedAsync(bt => bt.Id == badgeTypeId, cancellationToken);

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
                bi.Notes.Value))
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
