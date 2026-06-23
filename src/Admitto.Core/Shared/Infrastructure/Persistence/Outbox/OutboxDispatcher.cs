namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

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
    
    public async ValueTask<bool> DispatchOrphanedAsync(
        int batchSize,
        TimeSpan minimumAge,
        CancellationToken cancellationToken = default)
    {
        var eligibleCreatedAt = DateTimeOffset.UtcNow.Subtract(minimumAge);
        var outboxMessages = await dbContext.OutboxMessages
            .Where(m => m.State == OutboxMessageState.Pending && m.CreatedAt <= eligibleCreatedAt)
            .OrderBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (outboxMessages.Count == 0)
            return false;

        foreach (var outboxMessage in outboxMessages)
        {
            await messageSender.SendAsync(outboxMessage, cancellationToken);
            outboxMessage.State = OutboxMessageState.Sent;
        }

        if (dbContext is DbContext context)
            await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
