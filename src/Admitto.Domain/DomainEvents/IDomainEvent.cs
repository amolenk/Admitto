namespace Amolenk.Admitto.Domain.DomainEvents;

public interface IDomainEvent
{
    Guid DomainEventId { get; }
    
    DateTime OccurredOn { get; }
}
