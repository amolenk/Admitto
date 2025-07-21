using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IMessageOutbox
{
    void Enqueue(Command command);

    void Enqueue(DomainEvent domainEvent);
}