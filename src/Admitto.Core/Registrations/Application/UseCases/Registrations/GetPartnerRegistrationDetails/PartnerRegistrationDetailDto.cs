using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetPartnerRegistrationDetails;

public sealed record PartnerRegistrationDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    RegistrationStatus Status,
    IReadOnlyList<Guid> TicketTypeIds,
    IReadOnlyList<PartnerTicketDetailDto> Tickets,
    IReadOnlyList<PartnerWaitlistedTicketTypeDto> WaitlistedTicketTypes,
    IReadOnlyList<PartnerOfferedTicketTypeDto> OfferedTicketTypes,
    IReadOnlyDictionary<string, string> AdditionalDetails);

public sealed record PartnerTicketDetailDto(Guid Id, string Name);

public sealed record PartnerWaitlistedTicketTypeDto(Guid TicketTypeId, int Position);

/// <summary>
/// A ticket type for which the attendee holds an outstanding, unredeemed waitlist offer. Distinct from
/// <see cref="PartnerWaitlistedTicketTypeDto"/>: an offer has no queue position, and expires rather than moving
/// up a queue.
/// </summary>
public sealed record PartnerOfferedTicketTypeDto(Guid TicketTypeId, DateTimeOffset ExpiresAt);
