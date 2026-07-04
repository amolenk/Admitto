namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.AddBadgeType.AdminApi;

public sealed record AddBadgeTypeHttpRequest(
    string Name,
    string Kind,
    IReadOnlyList<Guid>? TicketTypeIds)
{
    internal AddBadgeTypeCommand ToCommand(Guid eventId, Guid teamId)
        => new(eventId, teamId, Name, Kind, TicketTypeIds ?? []);
}
