namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.AddBadgeType.AdminApi;

public sealed record AddBadgeTypeHttpRequest(
    string Name,
    string Kind,
    IReadOnlyList<Guid>? TicketTypeIds)
{
    internal AddBadgeTypeCommand ToCommand(Guid eventId)
        => new(eventId, Name, Kind, TicketTypeIds ?? []);
}
