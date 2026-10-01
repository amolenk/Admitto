namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetTicketTypes;

internal sealed record TicketTypeDto(
    Guid Id,
    string Name,
    string[] TimeSlots,
    int? PublicCapacity,
    int PublicUsedCapacity,
    int AdminUsedCount,
    bool SelfServiceEnabled,
    bool WaitlistEnabled,
    bool WaitlistMode,
    int ClaimWindowHours,
    int? MaxReconfirmationEmails);
