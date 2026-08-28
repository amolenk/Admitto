using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrations;

public sealed record RegistrationListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<TicketSummaryDto> Tickets,
    IReadOnlyDictionary<string, string> AdditionalDetails,
    DateTimeOffset CreatedAt,
    Guid RegistrationCycleId,
    uint RegistrationVersion,
    uint TicketCatalogVersion,
    RegistrationStatus Status,
    bool HasReconfirmed,
    DateTimeOffset? ReconfirmedAt);

public sealed record TicketSummaryDto(Guid Id, string Name);
