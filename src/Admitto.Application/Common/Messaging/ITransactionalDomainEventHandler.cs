using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Messaging;

/// <summary>
/// Represents a domain event handler that requires immediate consistency.
/// Handler runs within the same transaction as the domain aggregate.
/// </summary>
public interface ITransactionalDomainEventHandler<in TDomainEvent> : ITransactionalDomainEventHandler
    where TDomainEvent : DomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for transactional domain event handlers.
/// </summary>
public interface ITransactionalDomainEventHandler;
