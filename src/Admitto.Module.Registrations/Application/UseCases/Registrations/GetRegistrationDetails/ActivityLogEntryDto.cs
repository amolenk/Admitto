namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

public sealed record ActivityLogEntryDto(
    string ActivityType,
    DateTimeOffset OccurredAt,
    string? Metadata);
