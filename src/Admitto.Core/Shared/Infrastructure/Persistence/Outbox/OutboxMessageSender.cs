using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amolenk.Admitto.Core.Shared.Application;
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

        var cloudEvent = new JsonObject
        {
            ["specversion"] = "1.0",
            ["id"] = message.Id.ToString(),
            ["source"] = nameof(Admitto),
            ["type"] = message.Type,
            ["datacontenttype"] = "application/json",
            ["data"] = JsonNode.Parse(message.Payload.RootElement.GetRawText())
        };

        var propagationActivity = activity ?? Activity.Current;
        if (propagationActivity is { IdFormat: ActivityIdFormat.W3C })
        {
            cloudEvent[AdmittoActivitySource.TraceParentAttribute] = propagationActivity.Id!;
            if (!string.IsNullOrEmpty(propagationActivity.TraceStateString))
            {
                cloudEvent[AdmittoActivitySource.TraceStateAttribute] =
                    propagationActivity.TraceStateString;
            }
        }

        logger.LogInformation("Sending message to queue: {MessageType}", message.Type);

        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(cloudEvent.ToJsonString(JsonSerializerOptions.Web)))
        {
            ContentType = "application/cloudevents+json"
        };
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}
