using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Contracts;

/// <summary>
/// Cross-module facade exposed by the Email module so other modules can ask whether email is
/// configured for a given event without crossing a database boundary.
/// </summary>
/// <remarks>
/// The Email module reports an event as "configured" iff an <c>EventEmailSettings</c> record exists
/// for the event and its domain <c>IsValid</c> check passes. No SMTP connectivity probe is performed.
/// </remarks>
public interface IEventEmailFacade
{
    ValueTask<bool> IsEmailConfiguredAsync(
        TicketedEventId ticketedEventId,
        CancellationToken cancellationToken = default);
}
