namespace Amolenk.Admitto.Application.UseCases.Public.GetAvailability;

public record GetAvailabilityResponse(
    DateTimeOffset? RegistrationOpensAt,
    DateTimeOffset? RegistrationClosesAt,
    List<TicketTypeDto> TicketTypes,
    List<AdditionalDetailSchemaDto>? AdditionalDetailSchemas);

public record TicketTypeDto(string Slug, string Name, List<string> SlotNames, bool HasCapacity);

public record AdditionalDetailSchemaDto(string Name, string MaxLength, bool IsRequired);