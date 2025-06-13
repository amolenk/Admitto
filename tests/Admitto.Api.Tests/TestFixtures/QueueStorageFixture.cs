using Amolenk.Admitto.Infrastructure;
using Azure.Storage.Queues;

namespace Amolenk.Admitto.Application.Tests.TestFixtures;

public class QueueStorageFixture
{
    private QueueStorageFixture(string connectionString)
    {
        MessageQueue = new QueueClient(connectionString, Constants.AzureQueueStorage.DefaultQueueName);
        PrioMessageQueue = new QueueClient(connectionString, Constants.AzureQueueStorage.PrioQueueName);
    }
    
    public static async ValueTask<QueueStorageFixture> CreateAsync(TestingAspireAppHost appHost)
    {
        var connectionString = await appHost.GetConnectionString(Constants.AzureQueueStorage.ResourceName);
        if (connectionString is null)
        {
            throw new InvalidOperationException("Connection string for Azure Queue Storage not found.");
        }

        return new QueueStorageFixture(connectionString);
    }

    private QueueClient MessageQueue { get; }

    private QueueClient PrioMessageQueue { get; }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await MessageQueue.DeleteIfExistsAsync(cancellationToken);
        await PrioMessageQueue.DeleteIfExistsAsync(cancellationToken);

        await MessageQueue.CreateAsync(cancellationToken: cancellationToken);
        await PrioMessageQueue.CreateAsync(cancellationToken: cancellationToken);
    }
}