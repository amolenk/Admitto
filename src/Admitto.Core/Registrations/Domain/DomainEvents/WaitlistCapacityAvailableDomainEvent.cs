using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised when a catalog change leaves a ticket type in WaitlistMode with seats nobody holds: tickets released,
/// a waitlist hold released, <c>MaxCapacity</c> raised or <c>ReservedCapacity</c> lowered.
/// <see cref="AvailableCapacity"/> is the ticket type's unclamped availability right after that change; the
/// handler re-reads the catalog, since several changes in one unit of work each raise this event.
/// </summary>
public record WaitlistCapacityAvailableDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId,
    int AvailableCapacity) : DomainEvent;
