namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.GetBadgeInstances.AdminApi;

public sealed record BadgeInstanceListItemDto(
    Guid Id,
    string DisplayName,
    string Notes,
    uint Version);
