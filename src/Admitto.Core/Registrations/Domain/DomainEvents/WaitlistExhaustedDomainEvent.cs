using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record WaitlistExhaustedDomainEvent(
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId) : DomainEvent;
