namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.AddTicketType.AdminApi;

public sealed record AddTicketTypeHttpRequest(
    string Slug,
    string Name,
    string[] TimeSlots,
    int? Capacity,
    uint? ExpectedVersion)
{
    internal AddTicketTypeCommand ToCommand(Guid teamId, Guid eventId) =>
        new(
            teamId,
            eventId,
            Slug,
            Name,
            TimeSlots,
            Capacity,
            ExpectedVersion);
}
