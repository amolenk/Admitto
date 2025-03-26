using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Abstractions;

/// <summary>
/// Represents a domain event that requires immediate consistency.
/// These types of handlers run within the same transaction as the domain aggregate.
/// </summary>
public interface IImmediateDomainEventHandler
{
}

public interface IImmediateDomainEventHandler<in TDomainEvent> : IDomainEventHandler
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
