namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

public record GetTicketedEventResponse(
    string Slug,
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? RegistrationOpensAt,
    DateTimeOffset? RegistrationClosesAt,
    string BaseUrl,
    List<TicketTypeDto> TicketTypes,
    List<AdditionalDetailSchemaDto>? AdditionalDetailSchemas);

public record TicketTypeDto(string Slug, string Name, string SlotName, int MaxCapacity, int UsedCapacity);

public record AdditionalDetailSchemaDto(string Name, string MaxLength, bool IsRequired);