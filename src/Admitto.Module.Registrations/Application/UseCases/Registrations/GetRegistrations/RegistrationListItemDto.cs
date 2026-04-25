using Amolenk.Admitto.Module.Registrations.Contracts;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrations;

public sealed record RegistrationListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<TicketSummaryDto> Tickets,
    DateTimeOffset CreatedAt,
    RegistrationStatus Status,
    bool HasReconfirmed,
    DateTimeOffset? ReconfirmedAt);

public sealed record TicketSummaryDto(string Slug, string Name);
