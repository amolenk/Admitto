using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Abstractions;

/// <summary>
/// Represents a domain event that uses eventual consistency.
/// Handler runs after the domain aggregate transaction is completed.
/// </summary>
public interface IEventualDomainEventHandler<in TDomainEvent> : IEventualDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for eventual domain event handlers.
/// </summary>
public interface IEventualDomainEventHandler;


/// <summary>
/// Represents a domain event that requires immediate consistency.
/// Handler runs within the same transaction as the domain aggregate.
/// </summary>
public interface ITransactionalDomainEventHandler<in TDomainEvent> : ITransactionalDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for transactional domain event handlers.
/// </summary>
public interface ITransactionalDomainEventHandler;
