using Aspire.Hosting.Testing;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Amolenk.Admitto.Testing.Infrastructure.TestContexts;

public class MessagingTestContext
{
    private const string ServiceBusQueueName = "queue";

    private readonly ServiceBusAdministrationClient _administrationClient;

    public ServiceBusClient Client { get; }

    private MessagingTestContext(ServiceBusClient client, ServiceBusAdministrationClient administrationClient)
    {
        Client = client;
        _administrationClient = administrationClient;
    }

    public static async ValueTask<MessagingTestContext> CreateAsync(DistributedApplicationFactory appHost)
    {
        var emulatorConnectionString = await appHost.GetConnectionString("messaging");
        if (emulatorConnectionString is null)
        {
            throw new InvalidOperationException("Connection string for Service Bus not found.");
        }

        var emulatorEndpoint = appHost.GetEndpoint("messaging", "emulator");
        var healthEndpoint = appHost.GetEndpoint("messaging", "emulatorhealth");

        // Admin endpoint runs on the same port as the health endpoint that's already configured by Aspire.
        var adminConnectionString = emulatorConnectionString.Replace($":{emulatorEndpoint.Port}", $":{healthEndpoint.Port}");

        var serviceBusClient = new ServiceBusClient(emulatorConnectionString);
        var serviceBusAdministrationClient = new ServiceBusAdministrationClient(adminConnectionString);

        return new MessagingTestContext(serviceBusClient, serviceBusAdministrationClient);
    }

    public async Task ResetAsync()
    {
        await _administrationClient.DeleteQueueAsync(ServiceBusQueueName);
        await _administrationClient.CreateQueueAsync(ServiceBusQueueName);
    }
}
