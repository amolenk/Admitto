using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances;

internal sealed record GetBadgeInstancesQuery(Guid EventId, Guid TeamId, Guid BadgeTypeId)
    : Query<IReadOnlyList<BadgeInstanceListItemDto>>;
