using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventRequest(
    string Slug,
    string Name,
    string Website,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string BaseUrl,
    List<AdditionalDetailSchemaDto>? AdditionalDetailSchemas);

public record AdditionalDetailSchemaDto(string Name, int MaxLength, bool IsRequired);