using System.Diagnostics;
using Amolenk.Admitto.Core.Shared.Application;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

public class OutboxMessageSender(
    ServiceBusSender sender,
    ILogger<OutboxMessageSender> logger) : IOutboxMessageSender
{
    public async ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        using var activity = AdmittoActivitySource.ActivitySource.StartActivity(
            $"queue send {message.Type}",
            ActivityKind.Producer);
        activity?.AddTag("admitto.message.id", message.Id);
        activity?.AddTag("admitto.message.type", message.Type);
        activity?.AddTag("messaging.system", "AzureServiceBus");
        activity?.AddTag("messaging.destination.name", "queue");

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

        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(cloudEvent));
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}