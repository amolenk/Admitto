namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

public record GetTicketedEventResponse(
    string Slug,
    string Name,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime,
    DateTimeOffset RegistrationStartDateTime,
    DateTimeOffset RegistrationEndDateTime,
    IEnumerable<TicketTypeDto> TicketTypes);

public record TicketTypeDto(string Slug, string Name, string SlotName, int MaxCapacity, int UsedCapacity);
