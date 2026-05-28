namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.ListBadgeTypes.AdminApi;

public sealed record BadgeTypeListItemDto(
    Guid Id,
    string Name,
    string Kind,
    IReadOnlyList<Guid> TicketTypeIds,
    int InstanceCount);
