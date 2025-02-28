using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.MessageOutbox;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class CosmosOutboxMessageProvider(CosmosClient client) : IOutboxMessageProvider
{
    public async Task ExecuteAsync(Func<OutboxMessage, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken)
    {
        // TODO
        const string databaseId = "admitto";
        const string containerId = "core";
        
        var monitorContainer = client.GetContainer(databaseId, containerId);
        var leaseContainer = client.GetContainer(databaseId, "leases");

        var processor = monitorContainer
            .GetChangeFeedProcessorBuilder<CosmosDocument<OutboxMessage>>(
                "OutboxProcessor", 
                (changes, ct) => HandleChangesAsync(changes, messageHandler, ct))
            .WithInstanceName("local-worker")
            .WithLeaseContainer(leaseContainer)
            .WithStartTime(DateTime.MinValue.ToUniversalTime())
            .WithErrorNotification(HandleErrorAsync)
            .WithLeaseAcquireNotification(HandleLeaseAcquiredAsync)
            .WithLeaseReleaseNotification(HandleLeaseReleasedAsync)
            .Build();

        await processor.StartAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when the service is stopping.
        }
        
        await processor.StopAsync();
    }
    
    private async Task HandleChangesAsync(
        IReadOnlyCollection<CosmosDocument<OutboxMessage>> changes,
        Func<OutboxMessage, CancellationToken, ValueTask> messageHandler,
        CancellationToken cancellationToken)
    {
        // Not all the database changes are outbox messages, so we need to filter.
        var messageDocuments = changes
            .Where(c => c.Discriminator == nameof(OutboxMessage));

        foreach (var messageDocument in messageDocuments)
        {
            await messageHandler(messageDocument.Payload, cancellationToken);

            // TODO
            var container = client.GetContainer("admitto", "core");

            // Remove the message after it has been processed.
            await container.DeleteItemAsync<CosmosDocument<OutboxMessage>>(
                messageDocument.Id, 
                new PartitionKey(messageDocument.PartitionKey),
                cancellationToken: cancellationToken);
        }
    }

    private static Task HandleErrorAsync(string leaseToken, Exception exception)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }

    private static Task HandleLeaseAcquiredAsync(string leaseToken)
    {
        Console.WriteLine($"Lease {leaseToken} is acquired and will start processing");
        return Task.CompletedTask;
    }

    private static Task HandleLeaseReleasedAsync(string leaseToken)
    {
        Console.WriteLine($"Lease {leaseToken} is released and processing is stopped");
        return Task.CompletedTask;
    }
}
