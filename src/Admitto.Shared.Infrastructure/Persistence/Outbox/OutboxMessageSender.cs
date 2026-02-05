using Azure.Messaging;
using Azure.Storage.Queues;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence.Outbox;

public class OutboxMessageSender(
    QueueClient queueClient,
    ILogger<OutboxMessageSender> logger) : IOutboxMessageSender
{
    public async ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var cloudEvent = new CloudEvent(
            nameof(Admitto),
            message.Type,
            new BinaryData(message.Payload),
            "application/json")
        {
            Id = message.Id.ToString()
        };

        logger.LogInformation("Sending message to queue: {MessageType}", message.Type);

        await queueClient.SendMessageAsync(System.Text.Json.JsonSerializer.Serialize(cloudEvent), cancellationToken);
    }
}