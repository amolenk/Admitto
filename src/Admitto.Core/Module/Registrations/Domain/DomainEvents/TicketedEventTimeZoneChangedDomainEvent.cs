using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised when a ticketed event's time zone changes (initial set or admin update).
/// Mapped to the cross-module
/// <c>TicketedEventTimeZoneChangedIntegrationEvent</c> by
/// <c>RegistrationsMessagePolicy</c> so subscribers (e.g. Email scheduling) can stay
/// in sync.
/// </summary>
public sealed record TicketedEventTimeZoneChangedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    TimeZoneId TimeZone) : DomainEvent;
