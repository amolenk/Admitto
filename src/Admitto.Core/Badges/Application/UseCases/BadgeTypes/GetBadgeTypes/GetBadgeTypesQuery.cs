using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes.AdminApi;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes;

internal sealed record GetBadgeTypesQuery(Guid EventId, Guid TeamId) : Query<GetBadgeTypesResponse>;
