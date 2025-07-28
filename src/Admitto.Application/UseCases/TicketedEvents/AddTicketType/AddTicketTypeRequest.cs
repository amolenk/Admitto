namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.AddTicketType;

public record AddTicketTypeRequest(
    string Slug,
    string Name, 
    string SlotName, 
    int MaxCapacity);
