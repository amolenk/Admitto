using Amolenk.Admitto.Module.Registrations.Contracts;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

public sealed record RegistrationDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    RegistrationStatus Status,
    DateTimeOffset RegisteredAt,
    bool HasReconfirmed,
    DateTimeOffset? ReconfirmedAt,
    string? CancellationReason,
    IReadOnlyList<TicketDetailDto> Tickets,
    IReadOnlyDictionary<string, string> AdditionalDetails,
    IReadOnlyList<ActivityLogEntryDto> Activities);

public sealed record TicketDetailDto(string Slug, string Name);
