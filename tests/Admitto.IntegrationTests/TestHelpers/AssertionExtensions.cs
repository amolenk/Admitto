using System.Text.Json;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;
using Azure.Messaging;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

public static class AssertionExtensions
{
    [Obsolete("Use ShouldHaveProblemDetail instead.")]
    public static Task ShouldHaveProblemDetail(this HttpResponseMessage response, string errorKey)
    {
        return response.ShouldHaveProblemDetail(pd => pd.Errors.ShouldContainKey(errorKey));
    }
    
    public static async Task ShouldHaveProblemDetail(this HttpResponseMessage response, 
        params Action<ValidationProblemDetails>[] assertProblemDetails)
    {
        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        validationProblem.ShouldNotBeNull();

        foreach (var assertion in assertProblemDetails)
        {
            assertion(validationProblem);
        }
    }

    public static ValueTask ShouldContainMessageAsync<TMessage>(this QueueClient queueClient, Action<TMessage> assert, 
        TimeSpan? timeout = null)
        where TMessage : class
    {
        timeout ??= TimeSpan.FromSeconds(5);
        
        return Should.Eventually(async () =>
            {
                var message = await queueClient.PeekMessageAsync();
                message.ShouldNotBeNull();

                var cloudEvent = CloudEvent.Parse(message.Value.Body);
                cloudEvent.ShouldNotBeNull().Type.ShouldBe(typeof(TMessage).Name);

                var typedMessage = JsonSerializer.Deserialize<TMessage>(cloudEvent.Data!.ToString(), 
                    JsonSerializerOptions.Web);
                typedMessage.ShouldNotBeNull();

                assert(typedMessage);
            },
            timeout.Value);
    }
}