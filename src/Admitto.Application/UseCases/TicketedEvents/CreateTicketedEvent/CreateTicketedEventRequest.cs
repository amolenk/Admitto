namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventRequest(
    string Slug,
    string Name,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    DateTimeOffset RegistrationStartTime,
    DateTimeOffset RegistrationEndTime);

// TODO Cleanup
// public record TicketTypeDto(string Name, string SlotName, int MaxCapacity);
