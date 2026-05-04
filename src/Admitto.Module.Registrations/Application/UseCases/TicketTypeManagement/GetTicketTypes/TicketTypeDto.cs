namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes;

internal sealed record TicketTypeDto(
    string Slug,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    int UsedCapacity,
    bool IsCancelled,
    bool SelfServiceEnabled);
