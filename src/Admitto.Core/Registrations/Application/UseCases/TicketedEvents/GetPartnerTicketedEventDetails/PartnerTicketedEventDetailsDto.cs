namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetPartnerTicketedEventDetails;

/// <summary>
/// Partner-facing (event site) view of a ticketed event. Deliberately trimmed compared to the
/// admin <c>TicketedEventDetailsDto</c>: no internal id, team id, version, lifecycle status, or raw
/// policy windows are exposed. Only descriptive metadata the event site needs is included.
/// </summary>
public sealed record PartnerTicketedEventDetailsDto(
    string Name,
    string Slug,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone,
    bool IsRegistrationOpen,
    string? AllowedEmailDomain,
    IReadOnlyList<PartnerAdditionalDetailFieldDto> AdditionalDetailFields);

public sealed record PartnerAdditionalDetailFieldDto(string Key, string Name, int MaxLength);
