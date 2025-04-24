namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventRequest(
    string Name,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime,
    DateTimeOffset RegistrationStartDateTime,
    DateTimeOffset RegistrationEndDateTime,
    IEnumerable<TicketTypeDto>? TicketTypes);
    
public record TicketTypeDto(string Name, string SlotName, int MaxCapacity);