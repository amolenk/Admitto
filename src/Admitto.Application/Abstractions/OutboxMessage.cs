using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Dtos;

public class OutboxMessage
{
    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent)
    {
        return new DomainEventOutboxMessage(domainEvent);
    }

    public static OutboxMessage FromCommand(ICommand command)
    {
        return new CommandOutboxMessage(command);
    }
}

public class CommandOutboxMessage(ICommand command) : OutboxMessage
{
    public ICommand Command { get; private set; } = command;
}

public class DomainEventOutboxMessage(IDomainEvent domainEvent) : OutboxMessage
{
    public IDomainEvent DomainEvent { get; private set; } = domainEvent;
}