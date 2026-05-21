using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.ListBadgeTypes.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.ListBadgeTypes;

internal sealed record ListBadgeTypesQuery(Guid EventId) : Query<IReadOnlyList<BadgeTypeListItemDto>>;
