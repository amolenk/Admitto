namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.ListBadgeInstances.AdminApi;

public sealed record BadgeInstanceListItemDto(
    Guid Id,
    string DisplayName,
    string Notes);
