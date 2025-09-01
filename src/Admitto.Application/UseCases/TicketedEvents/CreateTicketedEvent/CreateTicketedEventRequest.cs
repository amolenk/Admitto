namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventRequest(
    string Slug,
    string Name,
    string Website,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string BaseUrl);
