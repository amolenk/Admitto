using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised when the capacity limit (MaxCapacity) is removed from a waitlist-enabled ticket type. A waitlist
/// requires bounded capacity, so the waitlist is switched off; everyone still waiting is offered a coupon.
/// </summary>
public record WaitlistCapacityLimitRemovedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId) : DomainEvent;
