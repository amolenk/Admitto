using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

public sealed class UnitOfWork<TDbContext>(
    TDbContext dbContext,
    IOutboxMessageSender outboxMessageSender,
    ILogger<UnitOfWork<TDbContext>> logger,
    IPostgresExceptionMapping? postgresExceptionMapping = null) : IUnitOfWork
    where TDbContext : DbContext
{
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result;
        try
        {
            // When saving changes, the DomainEventsInterceptor will dispatch all
            // pending domain events.
            result = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pge)
        {
            if (postgresExceptionMapping?.TryMapToError(pge, out var error) ?? false)
            {
                throw new BusinessRuleViolationException(error);
            }

            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleViolationException(ConcurrencyConflictError.Create());
        }

        if (result <= 0 || dbContext is not IOutboxDbContext outboxDbContext) return;

        try
        {
            // Best effort flush of the outbox messages to get fast dispatch.
            // Even if this fails, the messages are still in the outbox and will be retried later by the worker.
            var outboxDispatcher = new OutboxDispatcher(outboxDbContext, outboxMessageSender);
            if (await outboxDispatcher.DispatchTrackedAsync(cancellationToken))
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Best-effort outbox flush failed; pending messages will be retried by the worker.");
        }
    }
}
