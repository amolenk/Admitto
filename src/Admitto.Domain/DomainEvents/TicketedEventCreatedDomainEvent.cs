namespace Amolenk.Admitto.Domain.DomainEvents;

public record TicketedEventCreatedDomainEvent(Guid TeamId, string TicketedEventSlug) : DomainEvent;