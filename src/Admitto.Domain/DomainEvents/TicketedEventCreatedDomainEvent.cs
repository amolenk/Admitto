namespace Amolenk.Admitto.Domain.DomainEvents;

public record TicketedEventCreatedDomainEvent(Guid TeamId, Guid TicketedEventId, string TicketedEventSlug)
    : DomainEvent;