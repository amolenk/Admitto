namespace Amolenk.Admitto.Core.Registrations.Contracts;

public interface IRegistrationsFacade
{
    ValueTask<EventRegistrationSnapshotDto> GetEventRegistrationSnapshotAsync(
        Guid ticketedEventId,
        Guid registrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cross-module read query that returns the projections for every registration
    /// on the given ticketed event matching the supplied filters. Intentionally
    /// generic so multiple callers can reuse it without adding per-caller methods.
    /// </summary>
    Task<IReadOnlyList<RegistrationListItemDto>> GetRegistrationsAsync(
        Guid eventId,
        QueryRegistrationsDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the reconfirm trigger spec for the given event, or <c>null</c>
    /// when the event has no active reconfirm policy (or is not in
    /// <c>Active</c> lifecycle status). Used by the Email module's reconfirm
    /// trigger scheduler in response to policy- and time-zone-changed
    /// integration events.
    /// </summary>
    Task<ReconfirmTriggerSpecDto?> GetReconfirmTriggerSpecAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates reconfirm trigger specs for every active ticketed event
    /// that currently has a reconfirm policy. Used by the Email module's
    /// worker on startup to idempotently reconcile per-event triggers.
    /// </summary>
    Task<IReadOnlyList<ReconfirmTriggerSpecDto>> GetActiveReconfirmTriggerSpecsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the ordered additional detail schema fields for the given event.
    /// Used by the Badges module's CSV export handler to determine column order.
    /// </summary>
    Task<IReadOnlyList<AdditionalDetailFieldDto>> GetAdditionalDetailSchemaAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}
