namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.GetBadgeTypes.AdminApi;

public sealed record GetBadgeTypesResponse(
    uint EventVersion,
    IReadOnlyList<BadgeTypeListItemDto> BadgeTypes);
