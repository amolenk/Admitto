using Amolenk.Admitto.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Shared.Application.Messaging;

/// <summary>
/// Represents a domain event handler that requires immediate consistency.
/// Handler runs within the same transaction as the domain aggregate.
/// </summary>
public interface IDomainEventHandler<in TDomainEvent> : IDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for transactional domain event handlers.
/// </summary>
public interface IDomainEventHandler;