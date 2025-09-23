namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.UpdateTicketedEvent;

public record UpdateTicketedEventRequest(
    string? Name,
    string? Website,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? BaseUrl);
