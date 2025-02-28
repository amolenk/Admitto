using Amolenk.Admitto.Application.MessageOutbox;

namespace Admitto.OutboxProcessor;

public class Worker(MessageOutboxProcessor messageOutboxProcessor) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return messageOutboxProcessor.ExecuteAsync(stoppingToken);
    }
}