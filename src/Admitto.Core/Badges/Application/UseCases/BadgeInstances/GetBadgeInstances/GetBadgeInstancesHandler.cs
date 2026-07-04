using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances;

internal sealed class GetBadgeInstancesHandler(IBadgesWriteStore writeStore)
    : IQueryHandler<GetBadgeInstancesQuery, IReadOnlyList<BadgeInstanceListItemDto>>
{
    public async ValueTask<IReadOnlyList<BadgeInstanceListItemDto>> HandleAsync(
        GetBadgeInstancesQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(query.EventId);
        var teamId = TeamId.From(query.TeamId);
        var badgeTypeId = BadgeTypeId.From(query.BadgeTypeId);

        // Load BadgeEvent (untracked for guard)
        var badgeEvent = await writeStore.BadgeEvents.GetUntrackedAsync(
            be => be.Id == eventId && be.TeamId == teamId,
            cancellationToken);

        // Call aggregate method which enforces all business rules
        badgeEvent.EnsureCanManageInstances(badgeTypeId);

        var instances = await writeStore.BadgeInstances
            .AsNoTracking()
            .Where(bi => bi.TeamId == teamId && bi.EventId == eventId && bi.BadgeTypeId == badgeTypeId)
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
}
