using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Messaging;

/// <summary>
/// Represents a domain event handler that uses eventual consistency.
/// Handler runs after the domain aggregate transaction is completed.
/// </summary>
public interface IEventualDomainEventHandler<in TDomainEvent> : IEventualDomainEventHandler
    where TDomainEvent : DomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Marker interface for eventual domain event handlers.
/// </summary>
public interface IEventualDomainEventHandler;
