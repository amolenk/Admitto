namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.AddBadgeInstance.AdminApi;

public sealed record AddBadgeInstanceHttpRequest(string DisplayName, string? Notes)
{
    internal AddBadgeInstanceCommand ToCommand(Guid eventId, Guid teamId, Guid badgeTypeId)
        => new(eventId, teamId, badgeTypeId, DisplayName, Notes ?? string.Empty);
}
