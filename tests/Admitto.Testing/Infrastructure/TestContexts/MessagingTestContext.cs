using Aspire.Hosting.Testing;
using Azure.Messaging.ServiceBus;

namespace Amolenk.Admitto.Testing.Infrastructure.TestContexts;

public class MessagingTestContext
{
    private const string ServiceBusQueueName = "queue";

    public ServiceBusClient Client { get; }

    private MessagingTestContext(ServiceBusClient client)
    {
        Client = client;
    }

    public static async ValueTask<MessagingTestContext> CreateAsync(DistributedApplicationFactory appHost)
    {
        var emulatorConnectionString = await appHost.GetConnectionString("messaging");
        if (emulatorConnectionString is null)
        {
            throw new InvalidOperationException("Connection string for Service Bus not found.");
        }

        var serviceBusClient = new ServiceBusClient(emulatorConnectionString);

        return new MessagingTestContext(serviceBusClient);
    }

    /// <summary>
    /// Drains the queue and its dead-letter sub-queue rather than deleting and
    /// recreating the entity, so the Worker's long-lived
    /// <c>ServiceBusProcessor</c> link never has to detach and reconnect
    /// (recreating the queue 200+ times a run was a source of Service Bus
    /// flakiness against the running Worker).
    /// </summary>
    public async Task ResetAsync()
    {
        await DrainAsync(new ServiceBusReceiverOptions());
        await DrainAsync(new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
    }

    private async Task DrainAsync(ServiceBusReceiverOptions options)
    {
        options.ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete;
        await using var receiver = Client.CreateReceiver(ServiceBusQueueName, options);

        while (true)
        {
            var messages = await receiver.ReceiveMessagesAsync(
                maxMessages: 100,
                maxWaitTime: TimeSpan.FromMilliseconds(500));
            if (messages.Count == 0)
                break;
        }
    }
}
