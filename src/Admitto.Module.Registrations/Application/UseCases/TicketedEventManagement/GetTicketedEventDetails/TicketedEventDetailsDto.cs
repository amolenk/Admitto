using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails;

public sealed record TicketedEventDetailsDto(
    Guid Id,
    Guid TeamId,
    string Slug,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    EventLifecycleStatus Status,
    uint Version,
    bool IsRegistrationOpen,
    RegistrationPolicyDto? RegistrationPolicy,
    CancellationPolicyDto? CancellationPolicy,
    ReconfirmPolicyDto? ReconfirmPolicy);

public sealed record RegistrationPolicyDto(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    string? AllowedEmailDomain);

public sealed record CancellationPolicyDto(DateTimeOffset LateCancellationCutoff);

public sealed record ReconfirmPolicyDto(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int CadenceDays);
