using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes;

internal sealed class GetBadgeTypesHandler(IBadgesWriteStore writeStore)
    : IQueryHandler<GetBadgeTypesQuery, IReadOnlyList<BadgeTypeListItemDto>>
{
    public async ValueTask<IReadOnlyList<BadgeTypeListItemDto>> HandleAsync(
        GetBadgeTypesQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(query.EventId);

        var badgeTypes = await writeStore.BadgeTypes
            .AsNoTracking()
            .Where(bt => bt.EventId == eventId)
            .ToListAsync(cancellationToken);

        if (badgeTypes.Count == 0)
            return [];

        // Count instances per standalone badge type.
        var standaloneIds = badgeTypes
            .Where(bt => bt.Kind == BadgeKind.Standalone)
            .Select(bt => bt.Id)
            .ToList();

        Dictionary<BadgeTypeId, int> instanceCounts = [];
        if (standaloneIds.Count > 0)
        {
            instanceCounts = await writeStore.BadgeInstances
                .AsNoTracking()
                .Where(bi => standaloneIds.Contains(bi.BadgeTypeId))
                .GroupBy(bi => bi.BadgeTypeId)
                .Select(g => new { BadgeTypeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BadgeTypeId, x => x.Count, cancellationToken);
        }

        return badgeTypes
            .Select(bt => new BadgeTypeListItemDto(
                bt.Id.Value,
                bt.Name.Value,
                bt.Kind.ToString().ToLowerInvariant(),
                bt.TicketTypeIds.Select(id => id.Value).ToList(),
                instanceCounts.GetValueOrDefault(bt.Id, 0),
                bt.Version))
            .ToList();
    }
}
