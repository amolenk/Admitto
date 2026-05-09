namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetPublicTicketTypes;

public sealed record PublicTicketTypeDto(
    string Slug,
    string Name,
    string[] TimeSlots,
    int? MaxCapacity,
    int UsedCapacity);
