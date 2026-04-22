using System.Diagnostics;
using Amolenk.Admitto.Module.Shared.Application;
using Azure.Messaging;
using Azure.Storage.Queues;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

public class OutboxMessageSender(
    QueueClient queueClient,
    ILogger<OutboxMessageSender> logger) : IOutboxMessageSender
{
    public async ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        using var activity = AdmittoActivitySource.ActivitySource.StartActivity(
            $"queue send {message.Type}",
            ActivityKind.Producer);
        activity?.AddTag("admitto.message.id", message.Id);
        activity?.AddTag("admitto.message.type", message.Type);
        activity?.AddTag("messaging.system", "azure.storage.queue");
        activity?.AddTag("messaging.destination.name", queueClient.Name);

        var cloudEvent = new CloudEvent(
            nameof(Admitto),
            message.Type,
            new BinaryData(message.Payload),
            "application/json")
        {
            Id = message.Id.ToString()
        };

        var propagationActivity = activity ?? Activity.Current;
        if (propagationActivity is { IdFormat: ActivityIdFormat.W3C })
        {
            cloudEvent.ExtensionAttributes[AdmittoActivitySource.TraceParentAttribute] = propagationActivity.Id!;
            if (!string.IsNullOrEmpty(propagationActivity.TraceStateString))
            {
                cloudEvent.ExtensionAttributes[AdmittoActivitySource.TraceStateAttribute] = propagationActivity.TraceStateString;
            }
        }

        logger.LogInformation("Sending message to queue: {MessageType}", message.Type);

        await queueClient.SendMessageAsync(System.Text.Json.JsonSerializer.Serialize(cloudEvent), cancellationToken);
    }
}