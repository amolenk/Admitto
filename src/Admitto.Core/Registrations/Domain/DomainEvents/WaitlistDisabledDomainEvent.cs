using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised when an organizer explicitly disables the waitlist of a ticket type that keeps a bounded capacity.
/// <see cref="FreedSlots"/> is the number of public slots the same update freed while the ticket type was in
/// WaitlistMode; they are offered to the front of the queue before everyone still waiting is removed.
/// </summary>
public record WaitlistDisabledDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId,
    int FreedSlots) : DomainEvent;
