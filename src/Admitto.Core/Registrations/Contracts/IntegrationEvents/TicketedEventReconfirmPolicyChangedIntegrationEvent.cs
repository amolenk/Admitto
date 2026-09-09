using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Snapshot of a <c>TicketedEventReconfirmPolicy</c> as it appears at the moment
/// the policy is set or updated. Carried on
/// <see cref="TicketedEventReconfirmPolicyChangedIntegrationEvent"/> so subscribers
/// (e.g. the Email module's hourly evaluator) can update its projection without
/// a follow-up read against the Registrations module.
/// </summary>
public sealed record TicketedEventReconfirmPolicySnapshot(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int MinEmailIntervalHours,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd);

/// <summary>
/// Published by the Registrations module whenever a ticketed event's reconfirm
/// policy is set, updated, or cleared. The Email module consumes this to
/// update or clear the projected reconfirm policy.
/// </summary>
/// <param name="TeamId">Owning team.</param>
/// <param name="TicketedEventId">Ticketed event whose policy changed.</param>
/// <param name="TicketedEventVersion">Source aggregate version for stale-message detection.</param>
/// <param name="Policy">
/// New policy snapshot, or <c>null</c> when the policy has been cleared.
/// </param>
[method: JsonConstructor]
public sealed record TicketedEventReconfirmPolicyChangedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    uint TicketedEventVersion,
    TicketedEventReconfirmPolicySnapshot? Policy) : IntegrationEvent;
