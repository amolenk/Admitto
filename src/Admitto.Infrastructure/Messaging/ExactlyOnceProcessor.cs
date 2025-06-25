using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class ExactlyOnceProcessor(IProcessedMessageContext context, ILogger<ExactlyOnceProcessor> logger) 
    : IExactlyOnceProcessor
{
    public async ValueTask<bool> TryMarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var processedMessage = new ProcessedMessage(messageId);
            context.ProcessedMessages.Add(processedMessage);
            
            // This will throw if the message ID already exists due to primary key constraint
            await ((DbContext)context).SaveChangesAsync(cancellationToken);
            
            logger.LogDebug("Message {MessageId} marked as processed", messageId);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogDebug("Message {MessageId} was already processed", messageId);
            
            // Remove the entity from the change tracker since it failed to save
            var entry = ((DbContext)context).Entry(context.ProcessedMessages.Local.First(pm => pm.MessageId == messageId));
            entry.State = EntityState.Detached;
            
            return false;
        }
    }
    
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL unique constraint violation
        return ex.InnerException?.Message?.Contains("duplicate key") == true ||
               ex.InnerException?.Message?.Contains("23505") == true;
    }
}