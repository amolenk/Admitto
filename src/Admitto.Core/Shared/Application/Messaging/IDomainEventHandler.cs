using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

/// <summary>
/// Represents a domain event handler that runs as part of the unit of work transaction.
/// </summary>
public interface IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
