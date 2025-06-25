using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure;

public class UnitOfWork(ApplicationContext context, MessageOutbox outbox, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public async ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await context.SaveChangesAsync(cancellationToken);

            // Flush the outbox after saving changes to the database.
            if (await outbox.FlushAsync(cancellationToken))
            {
                // Save changes again to remove the processed outbox messages.
                await context.SaveChangesAsync(cancellationToken);
            }
            
            return result;
        }
        catch (DbUpdateException ex) when (IsProcessedMessageConstraintViolation(ex))
        {
            logger.LogDebug("Duplicate processed message detected, skipping transaction. {Error}", ex.Message);
            
            // If it's a processed message constraint violation, it means another instance 
            // already processed this message. We should skip this transaction entirely.
            throw new ProcessedMessageDuplicateException("Message was already processed by another instance", ex);
        }
    }
    
    private static bool IsProcessedMessageConstraintViolation(DbUpdateException ex)
    {
        // Check if the constraint violation is specifically for the processed_messages table
        var isConstraintViolation = ex.InnerException?.Message?.Contains("duplicate key") == true ||
                                   ex.InnerException?.Message?.Contains("23505") == true;
                                   
        var isProcessedMessagesTable = ex.InnerException?.Message?.Contains("processed_messages") == true ||
                                      ex.InnerException?.Message?.Contains("PK_processed_messages") == true;
                                      
        return isConstraintViolation && isProcessedMessagesTable;
    }
}