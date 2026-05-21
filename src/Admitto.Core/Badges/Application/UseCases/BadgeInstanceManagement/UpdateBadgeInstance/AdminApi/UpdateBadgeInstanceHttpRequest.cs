namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.UpdateBadgeInstance.AdminApi;

public sealed record UpdateBadgeInstanceHttpRequest(string DisplayName, string? Notes)
{
    internal UpdateBadgeInstanceCommand ToCommand(Guid eventId, Guid badgeTypeId, Guid badgeInstanceId)
        => new(eventId, badgeTypeId, badgeInstanceId, DisplayName, Notes ?? string.Empty);
}
