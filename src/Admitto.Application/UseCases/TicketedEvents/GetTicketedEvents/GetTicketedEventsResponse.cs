namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvents;

public record GetTicketedEventsResponse(TicketedEventDto[] TicketedEvents);

public record TicketedEventDto(string Slug, string Name, DateTimeOffset StartTime, DateTimeOffset EndTime,
    DateTimeOffset RegistrationStartTime, DateTimeOffset RegistrationEndTime);