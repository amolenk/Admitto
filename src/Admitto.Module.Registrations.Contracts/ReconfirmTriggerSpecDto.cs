namespace Amolenk.Admitto.Module.Registrations.Contracts;

/// <summary>
/// Cross-module read projection describing the inputs the Email module's
/// reconfirm scheduler needs to (re)register a per-event Quartz trigger:
/// owning team, event time zone, and the active reconfirm policy snapshot.
///
/// Only emitted for events whose lifecycle status is <c>Active</c> AND whose
/// reconfirm policy is currently set; consumers that receive a non-empty
/// result can treat it as "this trigger should exist". Event-id and time-zone
/// are required so the handler can rebuild the trigger without an additional
/// round-trip; the policy fields drive the cron and window.
/// </summary>
public sealed record ReconfirmTriggerSpecDto(
    Guid TeamId,
    Guid TicketedEventId,
    string TimeZone,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    int CadenceDays);
