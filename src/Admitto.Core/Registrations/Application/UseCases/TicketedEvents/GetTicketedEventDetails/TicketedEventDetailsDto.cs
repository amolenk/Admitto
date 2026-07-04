using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails;

public sealed record TicketedEventDetailsDto(
    Guid Id,
    Guid TeamId,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    string PublicSlug,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone,
    EventLifecycleStatus Status,
    uint Version,
    bool IsRegistrationOpen,
    RegistrationPolicyDto? RegistrationPolicy,
    ReconfirmPolicyDto? ReconfirmPolicy,
    WaitlistPolicyDto WaitlistPolicy,
    IReadOnlyList<AdditionalDetailFieldDto> AdditionalDetailSchema);

public sealed record AdditionalDetailFieldDto(string Key, string Name, int MaxLength);

public sealed record RegistrationPolicyDto(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    string? AllowedEmailDomain);

public sealed record ReconfirmPolicyDto(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int CadenceHours,
    int MinEmailIntervalHours);

public sealed record WaitlistPolicyDto(
    TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd);
