namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvents;

public record GetTicketedEventsResponse(TicketedEventDto[] TicketedEvents);

public record TicketedEventDto(
    string Slug,
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? RegistrationOpensAt,
    DateTimeOffset? RegistrationClosesAt);