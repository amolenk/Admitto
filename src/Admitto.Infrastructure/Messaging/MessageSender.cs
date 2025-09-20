using Amolenk.Admitto.Application.Common.Abstractions;
using Azure.Messaging;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class MessageSender(
    QueueClient queueClient,
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

        logger.LogInformation("Sending message to queue: {MessageType}", message.Type);

        await queueClient.SendMessageAsync(System.Text.Json.JsonSerializer.Serialize(cloudEvent), cancellationToken);
    }
}