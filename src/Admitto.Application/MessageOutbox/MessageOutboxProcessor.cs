using System.Text.Json;
using Amolenk.Admitto.Domain.DomainEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Application.MessageOutbox;

/// <summary>
/// Receives messages from the outbox and dispatches them to the appropriate handlers.
/// </summary>
public class MessageOutboxProcessor(IOutboxMessageProvider outboxMessageProvider, IServiceProvider serviceProvider)
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return outboxMessageProvider.ExecuteAsync(HandleMessageAsync, cancellationToken);
    }

    private async ValueTask HandleMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        
        var bodyJson = (JsonElement)message.Body;
        var bodyType = Type.GetType(message.Discriminator, true)!;

        var body = bodyJson.Deserialize(bodyType, JsonSerializerOptions.Web);

        switch (body)
        {
            case ICommand command:
                await HandleCommandAsync(command, scope.ServiceProvider, cancellationToken);
                break;
            case IDomainEvent domainEvent:
                await HandleDomainEventAsync(domainEvent, scope.ServiceProvider, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Cannot handle outbox message of type: {message.Discriminator}");
        }
    }
    
    private static ValueTask HandleCommandAsync(ICommand command, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        dynamic handler = serviceProvider.GetRequiredService(handlerType);

        return handler.HandleAsync((dynamic)command, cancellationToken);
    }
    
    private static ValueTask HandleDomainEventAsync(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        dynamic handler = serviceProvider.GetRequiredService(handlerType);

        return handler.HandleAsync((dynamic)domainEvent, cancellationToken);
    }
}
