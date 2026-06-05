using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record WaitlistModeActivatedDomainEvent(
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId) : DomainEvent;
