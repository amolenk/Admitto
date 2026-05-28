namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance.AdminApi;

public sealed record UpdateBadgeInstanceHttpRequest(string DisplayName, string? Notes)
{
    internal UpdateBadgeInstanceCommand ToCommand(Guid eventId, Guid teamId, Guid badgeTypeId, Guid badgeInstanceId)
        => new(eventId, teamId, badgeTypeId, badgeInstanceId, DisplayName, Notes ?? string.Empty);
}
