namespace Amolenk.Admitto.Domain.DomainEvents;

public record CrewMemberAddedDomainEvent(Guid TicketedEventId, Guid CrewMemberId) : DomainEvent;
