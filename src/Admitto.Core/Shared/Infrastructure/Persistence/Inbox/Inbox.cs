using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;

public sealed class Inbox(IInboxDbContext dbContext) : IInbox
{
    public void MarkAsProcessed<THandler>(IIntegrationEvent integrationEvent, THandler handler)
    {
        var messageKey = $"{integrationEvent.GetType().Name}.{typeof(THandler).Name}";

        var processedMessage = ProcessedMessage.Create(messageKey, DateTime.UtcNow);

        dbContext.ProcessedMessages.Add(processedMessage);
    }
}
