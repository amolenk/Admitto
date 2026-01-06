using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Messaging;

/// <summary>
/// Classes that implement this interface can enqueue messages in the outbox.
/// Enqueued messages are temporarily stored in the database and sent after the unit of work is committed.
/// </summary>
public interface IMessageOutbox
{
    void Enqueue(Command command);

    void Enqueue(ApplicationEvent domainEvent);
    
    void Enqueue(DomainEvent domainEvent);
}