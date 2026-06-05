using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record WaitlistCapacityFreedDomainEvent(
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId,
    int FreedSlots) : DomainEvent;
