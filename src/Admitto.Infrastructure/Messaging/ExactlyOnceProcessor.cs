using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class ExactlyOnceProcessor(IProcessedMessageContext context, ILogger<ExactlyOnceProcessor> logger) 
    : IExactlyOnceProcessor
{
    public async ValueTask<bool> TryMarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        // First check if the message has already been processed
        var existingMessage = await context.ProcessedMessages
            .FirstOrDefaultAsync(pm => pm.MessageId == messageId, cancellationToken);
            
        if (existingMessage != null)
        {
            logger.LogDebug("Message {MessageId} was already processed at {ProcessedAt}", 
                messageId, existingMessage.ProcessedAt);
            return false;
        }
        
        // Add the processed message to the context (will be committed with UnitOfWork)
        var processedMessage = new ProcessedMessage(messageId);
        context.ProcessedMessages.Add(processedMessage);
        
        logger.LogDebug("Message {MessageId} marked for processing", messageId);
        return true;
    }
}