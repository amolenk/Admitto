using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails;

public sealed record TicketedEventDetailsDto(
    Guid Id,
    Guid TeamId,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone,
    EventLifecycleStatus Status,
    uint Version,
    bool IsRegistrationOpen,
    RegistrationPolicyDto? RegistrationPolicy,
    CancellationPolicyDto? CancellationPolicy,
    ReconfirmPolicyDto? ReconfirmPolicy,
    IReadOnlyList<AdditionalDetailFieldDto> AdditionalDetailSchema);

public sealed record AdditionalDetailFieldDto(string Key, string Name, int MaxLength);

public sealed record RegistrationPolicyDto(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    string? AllowedEmailDomain);

public sealed record CancellationPolicyDto(DateTimeOffset LateCancellationCutoff);

public sealed record ReconfirmPolicyDto(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int CadenceDays);
