namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvent;

internal sealed record TicketedEventDto(
    string Slug,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status,
    uint Version);
