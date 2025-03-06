using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.Common.DTOs;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Infrastructure.Persistence;

namespace Admitto.OutboxProcessor;

/// <summary>
/// Receives messages from the outbox and dispatches them to the appropriate handlers.
/// </summary>
public class MessageOutboxWorker(PgOutboxMessageDispatcher outboxMessageDispatcher, IServiceProvider serviceProvider)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return outboxMessageDispatcher.ExecuteAsync(HandleMessageAsync, stoppingToken);
    }

    private async ValueTask HandleMessageAsync(OutboxMessageDto message, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        
        var bodyType = Type.GetType(message.Discriminator, true)!;
        
        var body = message.Payload.Deserialize(bodyType, JsonSerializerOptions.Web);
        
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
