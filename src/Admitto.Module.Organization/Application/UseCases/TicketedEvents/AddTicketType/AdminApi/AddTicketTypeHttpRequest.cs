namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.AddTicketType.AdminApi;

public sealed record AddTicketTypeHttpRequest(
    string Slug,
    string Name,
    bool IsSelfService,
    bool IsSelfServiceAvailable,
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
            IsSelfService,
            IsSelfServiceAvailable,
            TimeSlots,
            Capacity,
            ExpectedVersion);
}
