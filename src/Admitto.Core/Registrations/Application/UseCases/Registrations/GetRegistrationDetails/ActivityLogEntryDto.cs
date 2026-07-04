namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

public sealed record ActivityLogEntryDto(
    string ActivityType,
    DateTimeOffset OccurredAt,
    string? Metadata);
