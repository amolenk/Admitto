using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace Admitto.AppHost.Extensions.AzureStorage;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class AzureQueueCreatorHook(ILogger<AzureQueueCreatorHook> logger)
	: IDistributedApplicationEventingSubscriber
{
	private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);

	public Task SubscribeAsync(
		IDistributedApplicationEventing eventing,
		DistributedApplicationExecutionContext executionContext,
		CancellationToken cancellationToken)
	{
		eventing.Subscribe<AfterResourcesCreatedEvent>(async (@event, ct) =>
		{
			var queueResources = @event.Model.Resources.OfType<AzureQueueStorageResource>();
			foreach (var queueResource in queueResources)
			{
				if (!queueResource.TryGetAnnotationsOfType<AzureQueueAnnotation>(
					    out var queueAnnotations)) continue;

				var connectionString = await queueResource.ConnectionStringExpression.GetValueAsync(ct);
				if (connectionString is null) continue;

				await TryCreateQueuesUntilSuccessfulAsync(connectionString, 
					queueAnnotations.Select(a => a.QueueName).ToList(), ct);
			}
		});

		return Task.CompletedTask;
	}

	private async Task TryCreateQueuesUntilSuccessfulAsync(string connectionString, List<string> queueNames,
		CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				await CreateQueuesThatDoNotExistAsync(connectionString, queueNames, cancellationToken);
				return;
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to create queues");
				
				logger.LogWarning("Failed to create topics, retrying in {RetryDelayMs}ms",
					RetryDelay.TotalMilliseconds);
			}

			await Task.Delay(RetryDelay, cancellationToken);
		}
	}

	private async Task CreateQueuesThatDoNotExistAsync(string connectionString, IEnumerable<string> queueNames,
		CancellationToken cancellationToken)
	{
		var queueServiceClient = new QueueServiceClient(connectionString);

		foreach (var queueName in queueNames)
		{
			var queueClient = queueServiceClient.GetQueueClient(queueName);
			await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
			
			logger.LogInformation("Created queue '{QueueName}'", queueName);
		}
	}

}