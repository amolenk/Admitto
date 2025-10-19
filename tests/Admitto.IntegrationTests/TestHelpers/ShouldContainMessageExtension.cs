using System.Text.Json;
using Azure.Messaging;
using Azure.Storage.Queues;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

public static class ShouldContainMessageExtension
{
    public static ValueTask ShouldContainMessageAsync<TMessage>(this QueueClient queueClient, Action<TMessage> assert, 
        TimeSpan? timeout = null)
        where TMessage : class
    {
        timeout ??= TimeSpan.FromSeconds(5);
        
        return ShouldEventually.CompleteIn(async () =>
            {
                var response = await queueClient.PeekMessageAsync();
                response.ShouldNotBeNull();
                
                response.Value.ShouldNotBeNull();

                var cloudEvent = CloudEvent.Parse(response.Value.Body);
                cloudEvent.ShouldNotBeNull();

                var typedMessage = JsonSerializer.Deserialize<TMessage>(cloudEvent.Data!.ToString(), 
                    JsonSerializerOptions.Web);
                typedMessage.ShouldNotBeNull();

                assert(typedMessage);
            },
            timeout.Value,
            customMessage: $"Expected {typeof(TMessage).Name} message not found within {timeout}");
    }
}