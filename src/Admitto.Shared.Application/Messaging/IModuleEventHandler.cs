using Amolenk.Admitto.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Shared.Application.Messaging;

/// <summary>
/// Represents a domain event handler that runs after the events has been published through the outbox.
/// </summary>
public interface IOutboxDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
