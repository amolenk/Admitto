namespace Amolenk.Admitto.Application.Common.Messaging;

public interface ICommandSender
{
    ValueTask SendAsync(Command command, CancellationToken cancellationToken = default);

    void Enqueue(Command command);
}

// TODO Upgrade to full mediator and use in message processing as well
public class CommandSender(IMessageOutbox outbox, IServiceProvider serviceProvider, ILogger<CommandSender> logger)
    : ICommandSender
{
    public async ValueTask SendAsync(Command command, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        // ReSharper disable once ExplicitCallerInfoArgument
        using var activity = AdmittoActivitySource.ActivitySource.StartActivity("handle message");
        activity?.SetTag("admitto.message.id", command.CommandId);
        activity?.SetTag("admitto.message.type", commandType.Name);
        activity?.SetTag("admitto.handler.type", handlerType);
        
        logger.LogDebug(
            "Handling '{CommandType}' with handler '{HandlerType}'",
            commandType,
            handlerType);

        await ((dynamic)handler).HandleAsync((dynamic)command, cancellationToken);
    }
    
    public void Enqueue(Command command)
    {
        outbox.Enqueue(command);
    }
}