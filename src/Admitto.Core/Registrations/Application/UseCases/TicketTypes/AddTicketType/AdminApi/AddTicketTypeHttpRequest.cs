namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.AddTicketType.AdminApi;

public sealed record AddTicketTypeHttpRequest(
    string Name,
    bool SelfServiceEnabled = true,
    string[]? TimeSlots = null,
    int? MaxCapacity = null,
    bool WaitlistEnabled = false,
    int ClaimWindowHours = 8,
    int? MaxReconfirmationEmails = null)
{
    internal AddTicketTypeCommand ToCommand(Guid eventId, Guid teamId) => new(
        eventId,
        teamId,
        Name,
        TimeSlots ?? [],
        MaxCapacity,
        SelfServiceEnabled,
        WaitlistEnabled,
        ClaimWindowHours,
        MaxReconfirmationEmails);
}
