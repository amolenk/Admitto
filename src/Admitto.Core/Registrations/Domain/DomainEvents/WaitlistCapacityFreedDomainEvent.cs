using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record WaitlistCapacityFreedDomainEvent(
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId,
    int FreedSlots) : DomainEvent;
