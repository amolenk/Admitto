using Amolenk.Admitto.Application.Common.Abstractions;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class MessageSender(
    ServiceBusClient serviceBusClient,
    ILogger<MessageSender> logger) : IMessageSender
{
    public async ValueTask SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        var cloudEvent = new CloudEvent(
            nameof(Admitto),
            message.Type,
            new BinaryData(message.Data),
            "application/json")
        {
            Id = message.Id.ToString()
        };

        var sender = serviceBusClient.CreateSender("queue");
        
        logger.LogInformation("Sending message to queue: {MessageType}", message.Type);

        await sender.SendMessageAsync(new ServiceBusMessage(new BinaryData(cloudEvent)), cancellationToken);
    }
}