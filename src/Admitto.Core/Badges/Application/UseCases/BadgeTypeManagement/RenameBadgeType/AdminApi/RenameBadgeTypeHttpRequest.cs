namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.RenameBadgeType.AdminApi;

public sealed record RenameBadgeTypeHttpRequest(string Name)
{
    internal RenameBadgeTypeCommand ToCommand(Guid eventId, Guid badgeTypeId)
        => new(eventId, badgeTypeId, Name);
}
