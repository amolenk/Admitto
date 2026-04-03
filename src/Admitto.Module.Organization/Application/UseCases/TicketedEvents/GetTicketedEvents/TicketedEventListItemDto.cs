namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvents;

internal sealed record TicketedEventListItemDto(
    string Slug,
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status);
