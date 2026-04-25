using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Contracts;

public interface IRegistrationsFacade
{
    ValueTask<TicketedEventEmailContextDto> GetTicketedEventEmailContextAsync(
        Guid ticketedEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cross-module read query that returns the projections for every registration
    /// on the given ticketed event matching the supplied filters. Used by the Email
    /// module's bulk-email recipient resolver and intentionally generic so other
    /// future cross-module needs can reuse it.
    /// </summary>
    Task<IReadOnlyList<RegistrationListItemDto>> QueryRegistrationsAsync(
        TicketedEventId eventId,
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
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates reconfirm trigger specs for every active ticketed event
    /// that currently has a reconfirm policy. Used by the Email module's
    /// worker on startup to idempotently reconcile per-event triggers.
    /// </summary>
    Task<IReadOnlyList<ReconfirmTriggerSpecDto>> GetActiveReconfirmTriggerSpecsAsync(
        CancellationToken cancellationToken = default);
}
