namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetTicketTypes;

internal sealed record TicketTypeDto(
    Guid Id,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    int UsedCapacity,
    bool SelfServiceEnabled,
    bool WaitlistEnabled,
    bool WaitlistMode,
    int ClaimWindowHours,
    int? MaxReconfirmationEmails);
