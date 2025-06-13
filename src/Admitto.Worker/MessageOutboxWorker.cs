// using Amolenk.Admitto.Infrastructure.Messaging;
// using Amolenk.Admitto.Infrastructure.Persistence;
// using Azure.Messaging;
// using Azure.Storage.Queues;
// using Microsoft.Extensions.Options;
// using Polly;
// using Polly.Retry;
// using Polly.Telemetry;
//
// namespace Amolenk.Admitto.Worker;
//
// /// <summary>
// /// Receives messages from the PostgreSQL outbox using logical replication and publishes them as cloud events on Azure
// /// Storage Queues.
// /// </summary>
// public class MessageOutboxWorker(
//     PgOutboxMessageProcessor outboxMessageProcessor,
//     [FromKeyedServices("queues")] QueueServiceClient queueServiceClient,
//     IOptions<MessageOutboxWorkerOptions> options,
//     ILoggerFactory loggerFactory)
//     : BackgroundService
// {
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         await CreateRetryStrategy().ExecuteAsync(
//             ct => outboxMessageProcessor.ProcessMessagesAsync(HandleMessageAsync, ct), stoppingToken);
//     }
//
//     private async ValueTask HandleMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
//     {
//         var cloudEvent = new CloudEvent(nameof(Admitto), message.Type, new BinaryData(message.Data),
//             "application/json")
//         {
//             Id = message.Id.ToString()
//         };
//         
//         var queueClient = queueServiceClient.GetQueueClient(message.Priority ? "queue-prio" : "queue");
//         
//         await queueClient.SendMessageAsync(new BinaryData(cloudEvent), cancellationToken: cancellationToken);
//         
//         // TODO Remove from Outbox table
//     }
//     
//     private ResiliencePipeline CreateRetryStrategy()
//     {
//         var telemetryOptions = new TelemetryOptions
//         {
//             LoggerFactory = loggerFactory
//         };
//         
//         var workerOptions = options.Value;
//         
//         return new ResiliencePipelineBuilder { Name = "MessageOutboxWorker", InstanceName = "Default"}
//             .AddRetry(new RetryStrategyOptions
//             {
//                 BackoffType = workerOptions.RetryBackoffType,
//                 MaxDelay = workerOptions.MaxRetryDelay,
//                 MaxRetryAttempts = workerOptions.MaxRetryAttempts
//             })
//             .ConfigureTelemetry(telemetryOptions)
//             .Build();
//     }
// }
