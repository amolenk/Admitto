using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when the <c>TimeZone</c> of a ticketed
/// event changes. The Email module's reconfirm scheduler consumes this to
/// atomically replace the per-event Quartz trigger with one keyed to the new
/// IANA zone so cron cadences continue to fire at the same local hour.
/// </summary>
/// <param name="TeamId">Owning team.</param>
/// <param name="TicketedEventId">Ticketed event whose time zone changed.</param>
/// <param name="TimeZone">New IANA time-zone id (e.g. <c>Europe/Amsterdam</c>).</param>
public sealed record TicketedEventTimeZoneChangedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    string TimeZone) : IntegrationEvent;
