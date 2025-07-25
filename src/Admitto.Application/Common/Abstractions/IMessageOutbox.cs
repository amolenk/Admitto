using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IMessageOutbox
{
    void Enqueue(ICommand command, bool priority = false);

    void Enqueue(IDomainEvent domainEvent, bool priority = false);
}