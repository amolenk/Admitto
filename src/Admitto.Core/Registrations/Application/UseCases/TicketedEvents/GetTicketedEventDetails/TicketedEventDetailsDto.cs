using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails;

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
    ReconfirmPolicyDto? ReconfirmPolicy,
    IReadOnlyList<AdditionalDetailFieldDto> AdditionalDetailSchema,
    TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd);

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
