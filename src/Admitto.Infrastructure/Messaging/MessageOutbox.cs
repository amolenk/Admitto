using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.Common.Core;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Infrastructure.Persistence;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class MessageOutbox(ApplicationContext context, IMessageSender messageSender) : IMessageOutbox
{
    public void Enqueue(Command command)
    {
        context.Outbox.Add(Message.FromCommand(command));
    }
    
    public void Enqueue(DomainEvent domainEvent)
    {
        context.Outbox.Add(Message.FromDomainEvent(domainEvent));
    }

    public void Enqueue(ApplicationEvent applicationEvent)
    {
        context.Outbox.Add(Message.FromApplicationEvent(applicationEvent));
    }

    public async ValueTask<bool> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!context.Outbox.Any())
        {
            return false;
        }
        
        foreach (var outboxMessage in context.Outbox)
        {
            await messageSender.SendAsync(outboxMessage, cancellationToken);
            context.Outbox.Remove(outboxMessage);
        }

        return true;
    }
}
