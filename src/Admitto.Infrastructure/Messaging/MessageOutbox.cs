using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Infrastructure.Persistence;
using Azure.Messaging;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class MessageOutbox(
    ApplicationContext context, [FromKeyedServices("queues")] QueueServiceClient queueServiceClient)
    : IMessageOutbox
{
    public void Enqueue(ICommand command, bool priority)
    {
        context.Outbox.Add(OutboxMessage.FromCommand(command, priority));
    }

    public void Enqueue(IDomainEvent domainEvent, bool priority = false)
    {
        context.Outbox.Add(OutboxMessage.FromDomainEvent(domainEvent, priority));
    }

    public async ValueTask<bool> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!context.Outbox.Any())
        {
            return false;
        }
        
        foreach (var outboxMessage in context.Outbox)
        {
            await PublishMessageAsync(outboxMessage, cancellationToken);
            context.Outbox.Remove(outboxMessage);
        }

        return true;
    }
    
    private async ValueTask PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var cloudEvent = new CloudEvent(nameof(Admitto), message.Type, new BinaryData(message.Data),
            "application/json")
        {
            Id = message.Id.ToString()
        };
        
        var queueClient = queueServiceClient.GetQueueClient(message.Priority ? "queue-prio" : "queue");
        
        await queueClient.SendMessageAsync(new BinaryData(cloudEvent), cancellationToken: cancellationToken);
    }
}