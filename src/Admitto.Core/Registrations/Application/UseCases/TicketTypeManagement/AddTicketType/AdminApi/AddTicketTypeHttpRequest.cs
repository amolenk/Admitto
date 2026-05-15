namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType.AdminApi;

public sealed record AddTicketTypeHttpRequest(
    string Name,
    bool SelfServiceEnabled = true,
    string[]? TimeSlots = null,
    int? MaxCapacity = null)
{
    internal AddTicketTypeCommand ToCommand(Guid eventId) => new(
        eventId,
        Name,
        TimeSlots ?? [],
        MaxCapacity,
        SelfServiceEnabled);
}
