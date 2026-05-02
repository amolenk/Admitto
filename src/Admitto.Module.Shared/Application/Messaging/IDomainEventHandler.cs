using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

/// <summary>
/// Non-generic base — enables type-erased dispatch from infrastructure (e.g., DomainEventsInterceptor)
/// without reflection or dynamic.
/// </summary>
public interface IDomainEventHandler
{
    ValueTask HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a domain event handler that runs as part of the unit of work transaction.
/// </summary>
public interface IDomainEventHandler<TDomainEvent> : IDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);

    // Bridge the non-generic interface to the typed overload.
    ValueTask IDomainEventHandler.HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
        => HandleAsync((TDomainEvent)domainEvent, cancellationToken);
}
