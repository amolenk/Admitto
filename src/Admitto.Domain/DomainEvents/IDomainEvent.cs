using MediatR;

namespace Amolenk.Admitto.Domain.DomainEvents;

public interface IDomainEvent : INotification
{
    Guid DomainEventId { get; }
    
    DateTime OccurredOn { get; }
}
