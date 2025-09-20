namespace Admitto.AppHost.Extensions.AzureStorage;

public class AzureQueueAnnotation : IResourceAnnotation
{
    public required string QueueName { get; init; }
}