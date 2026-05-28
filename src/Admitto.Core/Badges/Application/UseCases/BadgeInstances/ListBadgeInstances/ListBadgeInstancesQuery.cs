using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.ListBadgeInstances.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.ListBadgeInstances;

internal sealed record ListBadgeInstancesQuery(Guid EventId, Guid TeamId, Guid BadgeTypeId)
    : Query<IReadOnlyList<BadgeInstanceListItemDto>>;
