namespace Amolenk.Admitto.Core.Registrations.Contracts;

public interface IRegistrationsFacade
{
    /// <summary>
    /// Cross-module read query that returns the projections for every registration
    /// on the given ticketed event matching the supplied filters. Intentionally
    /// generic so multiple callers can reuse it without adding per-caller methods.
    /// </summary>
    Task<IReadOnlyList<RegistrationListItemDto>> GetRegistrationsAsync(
        Guid teamId,
        Guid eventId,
        QueryRegistrationsDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the ordered additional detail schema fields for the given event.
    /// Used by the Badges module's CSV export handler to determine column order.
    /// </summary>
    Task<IReadOnlyList<AdditionalDetailFieldDto>> GetAdditionalDetailSchemaAsync(
        Guid teamId,
        Guid eventId,
        CancellationToken cancellationToken = default);
}
