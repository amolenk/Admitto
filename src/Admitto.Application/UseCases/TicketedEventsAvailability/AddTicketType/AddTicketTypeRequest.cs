namespace Amolenk.Admitto.Application.UseCases.TicketedEventsAvailability.AddTicketType;

public record AddTicketTypeRequest(
    string Slug,
    string Name, 
    string SlotName, 
    int MaxCapacity);
