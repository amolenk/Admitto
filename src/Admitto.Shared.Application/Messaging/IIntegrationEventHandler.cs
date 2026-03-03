using Amolenk.Admitto.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Shared.Application.Messaging;

/// <summary>
/// Represents a domain event handler that runs as part of the unit of work transaction.
/// </summary>
public interface IInTransactionDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    ValueTask HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
