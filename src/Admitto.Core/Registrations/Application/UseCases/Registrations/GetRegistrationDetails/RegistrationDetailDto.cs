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
    DateTimeOffset? CheckedInAt,
    string? CancellationReason,
    IReadOnlyList<TicketDetailDto> Tickets,
    IReadOnlyList<WaitlistEntryDetailDto> WaitlistEntries,
    IReadOnlyDictionary<string, string> AdditionalDetails,
    IReadOnlyList<ActivityLogEntryDto> Activities);

public sealed record TicketDetailDto(Guid Id, string Name);

/// <summary>
/// An attendee's current waitlist selection for one ticket type. <paramref name="Position"/> is set while the
/// entry is actively queued; <paramref name="IsOffered"/> and <paramref name="OfferExpiresAt"/> are set instead
/// while the entry holds an outstanding, unredeemed coupon offer (still a current selection until the offer is
/// redeemed, withdrawn, or expires).
/// </summary>
public sealed record WaitlistEntryDetailDto(
    string TicketTypeName,
    int? Position,
    bool IsOffered,
    DateTimeOffset? OfferExpiresAt);
