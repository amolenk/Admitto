namespace Amolenk.Admitto.Domain.DomainEvents;

public record ReconfirmPolicyUpdatedDomainEvent(Guid TeamId, Guid TicketedEventId)
    : DomainEvent;