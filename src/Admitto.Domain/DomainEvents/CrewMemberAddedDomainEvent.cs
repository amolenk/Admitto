namespace Amolenk.Admitto.Domain.DomainEvents;

public record CrewMemberAddedDomainEvent(Guid TicketedEventId, Guid CrewMemberId) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}