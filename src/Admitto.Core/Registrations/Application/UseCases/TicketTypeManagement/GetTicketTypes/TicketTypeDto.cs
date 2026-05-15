namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes;

internal sealed record TicketTypeDto(
    Guid Id,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    int UsedCapacity,
    bool IsCancelled,
    bool SelfServiceEnabled);
