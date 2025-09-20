using Aspire.Hosting.Azure;
using Aspire.Hosting.Lifecycle;

namespace Admitto.AppHost.Extensions.AzureStorage;

public static class AzureQueueStorageBuilderExtensions
{
    public static IResourceBuilder<T> CreateQueue<T>(
        this IResourceBuilder<T> builder,
        string queueName)
        where T : AzureQueueStorageResource
    {
        builder.WithAnnotation(new AzureQueueAnnotation
        {
            QueueName = queueName
        });
        
        builder.ApplicationBuilder.Services.TryAddLifecycleHook<AzureQueueCreatorHook>();

        return builder;
    }
}