using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

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

public sealed record TicketDetailDto(Guid Id, string Name);
