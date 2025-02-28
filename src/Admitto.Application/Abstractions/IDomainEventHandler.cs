using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Abstractions;

public interface IDomainEventHandler
{
}

public interface IDomainEventHandler<in TDomainEvent> : IDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
