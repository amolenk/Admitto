using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

/// <summary>
/// Represents a domain event handler that runs as part of the unit of work transaction.
/// </summary>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
