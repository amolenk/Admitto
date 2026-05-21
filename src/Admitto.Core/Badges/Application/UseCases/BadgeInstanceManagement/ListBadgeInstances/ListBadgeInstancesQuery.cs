using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.ListBadgeInstances.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.ListBadgeInstances;

internal sealed record ListBadgeInstancesQuery(Guid EventId, Guid BadgeTypeId)
    : Query<IReadOnlyList<BadgeInstanceListItemDto>>;
