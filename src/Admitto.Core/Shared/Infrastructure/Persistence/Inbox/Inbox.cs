using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;

public sealed class Inbox(IInboxDbContext dbContext) : IInbox
{
    public async ValueTask<bool> TryMarkAsProcessedByAsync<THandler>(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var messageKey = $"{integrationEvent.IntegrationEventId:N}.{typeof(THandler).FullName}";

        var alreadyProcessed = await dbContext.ProcessedMessages
            .AnyAsync(x => x.MessageKey == messageKey, cancellationToken);

        if (alreadyProcessed)
            return false;

        var processedMessage = ProcessedMessage.Create(messageKey, DateTime.UtcNow);

        dbContext.ProcessedMessages.Add(processedMessage);
        return true;
    }
}
