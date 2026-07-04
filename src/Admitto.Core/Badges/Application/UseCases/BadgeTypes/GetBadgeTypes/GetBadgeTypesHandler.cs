using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes;

internal sealed class GetBadgeTypesHandler(IBadgesWriteStore writeStore)
    : IQueryHandler<GetBadgeTypesQuery, GetBadgeTypesResponse>
{
    public async ValueTask<GetBadgeTypesResponse> HandleAsync(
        GetBadgeTypesQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(query.EventId);
        var teamId = TeamId.From(query.TeamId);

        // Load BadgeEvent (untracked)
        var badgeEvent = await writeStore.BadgeEvents.GetUntrackedAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);

        // Get instance counts for standalone badge types
        var standaloneIds = badgeEvent.BadgeTypes
            .Where(bt => bt.Kind == BadgeKind.Standalone)
            .Select(bt => bt.Id)
            .ToList();

        Dictionary<BadgeTypeId, int> instanceCounts = [];
        if (standaloneIds.Count > 0)
        {
            instanceCounts = await writeStore.BadgeInstances
                .AsNoTracking()
                .Where(bi => bi.TeamId == teamId && bi.EventId == eventId && standaloneIds.Contains(bi.BadgeTypeId))
                .GroupBy(bi => bi.BadgeTypeId)
                .Select(g => new { BadgeTypeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BadgeTypeId, x => x.Count, cancellationToken);
        }

        // Project badge types to DTOs
        var badgeTypes = badgeEvent.BadgeTypes
            .Select(bt => new BadgeTypeListItemDto(
                bt.Id.Value,
                bt.Name.Value,
                bt.Kind.ToString().ToLowerInvariant(),
                bt.TicketTypeIds.Select(id => id.Value).ToList(),
                instanceCounts.GetValueOrDefault(bt.Id, 0)))
            .ToList();

        return new GetBadgeTypesResponse(badgeEvent.Version, badgeTypes);
    }
}
