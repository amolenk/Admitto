using System.Diagnostics;
using System.Text.Json;
using Amolenk.Admitto.Module.Shared.Application;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Contracts;
using Azure.Messaging;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Messaging;

/// <summary>
/// Parses a CloudEvent pulled from the Azure Storage Queue, restores the
/// W3C trace context written by <c>OutboxMessageSender</c>, deserializes the
/// payload to its CLR type and routes it to the matching router.
/// </summary>
internal sealed partial class QueueMessageDispatcher(
    MessageTypeRegistry registry,
    IntegrationEventRouter integrationEventRouter,
    ModuleEventRouter moduleEventRouter,
    ILogger<QueueMessageDispatcher> logger)
{
    public async ValueTask DispatchAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        if (!registry.TryResolve(cloudEvent.Type, out var entry))
        {
            LogUnknownMessageType(logger, cloudEvent.Type);
            // Don't crash the consumer on an unknown message — let it be deleted
            // so it doesn't poison the queue. Persisting to a dead-letter store
            // is a follow-up.
            return;
        }

        var traceParent = cloudEvent.ExtensionAttributes.TryGetValue(
            AdmittoActivitySource.TraceParentAttribute,
            out var tp) ? tp as string : null;
        var traceState = cloudEvent.ExtensionAttributes.TryGetValue(
            AdmittoActivitySource.TraceStateAttribute,
            out var ts) ? ts as string : null;

        using var activity = AdmittoActivitySource.ActivitySource.StartActivity(
            $"queue receive {cloudEvent.Type}",
            ActivityKind.Consumer,
            traceParent ?? string.Empty);
        if (activity is not null && !string.IsNullOrEmpty(traceState))
        {
            activity.TraceStateString = traceState;
        }
        activity?.AddTag("admitto.message.type", cloudEvent.Type);
        activity?.AddTag("admitto.message.id", cloudEvent.Id);
        activity?.AddTag("admitto.module.name", entry.ModuleName);

        var payload = cloudEvent.Data?.ToString() ?? "{}";

        try
        {
            switch (entry.Kind)
            {
                case MessageTypeRegistry.MessageKind.IntegrationEvent:
                {
                    var integrationEvent = (IIntegrationEvent)JsonSerializer.Deserialize(
                        payload,
                        entry.ClrType,
                        JsonSerializerOptions.Web)!;
                    await integrationEventRouter.DispatchAsync(integrationEvent, cancellationToken);
                    break;
                }
                case MessageTypeRegistry.MessageKind.ModuleEvent:
                {
                    var moduleEvent = (IModuleEvent)JsonSerializer.Deserialize(
                        payload,
                        entry.ClrType,
                        JsonSerializerOptions.Web)!;
                    await moduleEventRouter.DispatchAsync(moduleEvent, entry.ModuleName, cancellationToken);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            throw;
        }
    }

    [LoggerMessage(
        LogLevel.Warning,
        "Received message of unknown type '{MessageType}'; discarding.")]
    static partial void LogUnknownMessageType(ILogger<QueueMessageDispatcher> logger, string messageType);
}
