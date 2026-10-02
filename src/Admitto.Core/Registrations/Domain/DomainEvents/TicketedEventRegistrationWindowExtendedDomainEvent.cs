using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised when a ticketed event's registration window is moved to close later, or cleared. Seats freed while
/// registration was closed weren't offered to the waitlist, so this re-runs the waitlist check for the event.
/// </summary>
public record TicketedEventRegistrationWindowExtendedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId) : DomainEvent;
