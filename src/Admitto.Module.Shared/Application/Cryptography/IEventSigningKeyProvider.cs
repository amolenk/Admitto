using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Shared.Application.Cryptography;

/// <summary>
/// Supplies the per-event HMAC signing key for a <see cref="TicketedEventId"/>.
/// Declared in Shared so any module can sign or verify event-scoped tokens via
/// <see cref="ISigningService"/> without taking a Registrations dependency; the
/// concrete implementation lives in the Registrations module, which owns the
/// <c>TicketedEvent</c> aggregate.
/// </summary>
public interface IEventSigningKeyProvider
{
    /// <summary>
    /// Returns the raw signing-key bytes for <paramref name="eventId"/>. Implementations
    /// SHOULD cache results in-process. Throws when the event id has no row.
    /// </summary>
    ValueTask<ReadOnlyMemory<byte>> GetKeyAsync(TicketedEventId eventId, CancellationToken cancellationToken);
}
