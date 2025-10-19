using Amolenk.Admitto.Infrastructure;
using Azure.Storage.Queues;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Fixtures;

public class QueueStorageTestFixture
{
    private QueueStorageTestFixture(string connectionString)
    {
        MessageQueue = new QueueClient(connectionString, Constants.AzureQueueStorage.DefaultQueueName);
    }
    
    public static async ValueTask<QueueStorageTestFixture> CreateAsync(TestingAspireAppHost appHost)
    {
        var connectionString = await appHost.GetConnectionString("queues");
        if (connectionString is null)
        {
            throw new InvalidOperationException("Connection string for Azure Queue Storage not found.");
        }

        return new QueueStorageTestFixture(connectionString);
    }

    public QueueClient MessageQueue { get; }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await MessageQueue.DeleteIfExistsAsync(cancellationToken);
        await MessageQueue.CreateAsync(cancellationToken: cancellationToken);
    }
}