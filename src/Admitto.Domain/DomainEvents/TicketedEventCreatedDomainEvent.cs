namespace Amolenk.Admitto.Domain.DomainEvents;

public record TicketedEventCreatedDomainEvent(Guid TeamId, string EventSlug) : DomainEvent;