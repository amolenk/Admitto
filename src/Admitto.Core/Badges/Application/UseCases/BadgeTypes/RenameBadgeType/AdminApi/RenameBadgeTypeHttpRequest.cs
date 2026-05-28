namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.RenameBadgeType.AdminApi;

public sealed record RenameBadgeTypeHttpRequest(string Name)
{
    internal RenameBadgeTypeCommand ToCommand(Guid eventId, Guid teamId, Guid badgeTypeId)
        => new(eventId, teamId, badgeTypeId, Name);
}
