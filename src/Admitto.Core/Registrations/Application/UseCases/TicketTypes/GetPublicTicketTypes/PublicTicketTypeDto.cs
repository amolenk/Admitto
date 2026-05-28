namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetPublicTicketTypes;

public sealed record PublicTicketTypeDto(
    Guid Id,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    int UsedCapacity,
    bool WaitlistEnabled,
    bool WaitlistMode);
