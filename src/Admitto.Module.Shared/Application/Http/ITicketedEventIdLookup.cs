namespace Amolenk.Admitto.Module.Shared.Application.Http;

/// <summary>
/// Cross-module abstraction used by <see cref="IOrganizationScopeResolver"/> to resolve
/// the Registrations-owned <c>TicketedEventId</c> for a given <c>(teamId, eventSlug)</c>
/// pair. Implemented by the Registrations module so that the Organization module can
/// resolve request scopes without referencing Registrations internals.
/// </summary>
public interface ITicketedEventIdLookup
{
    ValueTask<Guid> GetTicketedEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default);
}
