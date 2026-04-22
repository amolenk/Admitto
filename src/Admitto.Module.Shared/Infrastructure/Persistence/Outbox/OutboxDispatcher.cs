using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

internal class OutboxDispatcher(IOutboxDbContext dbContext, IOutboxMessageSender messageSender)
{
    public async ValueTask<bool> DispatchTrackedAsync(CancellationToken cancellationToken = default)
    {
        // Outbox messages are appended by the DomainEventsInterceptor during SavingChangesAsync,
        // so by the time this runs (immediately after SaveChangesAsync) their EF state is Unchanged.
        var outboxMessages = dbContext.ChangeTracker.Entries<OutboxMessage>()
            .Where(e => e.State != EntityState.Deleted && e.Entity.State == OutboxMessageState.Pending)
            .Select(e => e.Entity)
            .ToList();

        if (outboxMessages.Count == 0)
        {
            return false;
        }

        foreach (var outboxMessage in outboxMessages)
        {
            await messageSender.SendAsync(outboxMessage, cancellationToken);

            outboxMessage.State = OutboxMessageState.Sent;
        }

        return true;
    }
    
    public ValueTask<bool> DispatchOrphanedAsync(CancellationToken cancellationToken = default)
    {
        // TODO Dispatches orphaned outbox messages that were not tracked by the current DbContext.
        throw new NotImplementedException();
    }
}