namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.AddBadgeInstance.AdminApi;

public sealed record AddBadgeInstanceHttpRequest(string DisplayName, string? Notes)
{
    internal AddBadgeInstanceCommand ToCommand(Guid eventId, Guid badgeTypeId)
        => new(eventId, badgeTypeId, DisplayName, Notes ?? string.Empty);
}
