using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.ListBadgeTypes.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.ListBadgeTypes;

internal sealed record ListBadgeTypesQuery(Guid EventId, Guid TeamId) : Query<IReadOnlyList<BadgeTypeListItemDto>>;
