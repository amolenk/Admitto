using Amolenk.Admitto.Application.Common.Abstractions;
using Azure.Messaging;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class MessageSender(
    [FromKeyedServices("queues")] QueueServiceClient queueServiceClient,
    ILogger<MessageSender> logger) : IMessageSender
{
    public async ValueTask SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        var cloudEvent = new CloudEvent(
            nameof(Admitto),
            message.Type,
            new BinaryData(message.Data),
            "application/json")
        {
            Id = message.Id.ToString()
        };

        var queueClient = queueServiceClient.GetQueueClient(message.Priority ? "queue-prio" : "queue");

        logger.LogInformation("Sending message to queue: {MessageType}", message.Type);

        await queueClient.SendMessageAsync(new BinaryData(cloudEvent), cancellationToken: cancellationToken);
    }
}