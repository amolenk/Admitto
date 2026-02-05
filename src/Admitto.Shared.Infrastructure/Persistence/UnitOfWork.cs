using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Application.Persistence;
using Amolenk.Admitto.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence;

public sealed class UnitOfWork<TDbContext>(
    TDbContext dbContext,
    IMediator mediator,
    IOutboxMessageSender outboxMessageSender,
    IPostgresExceptionMapper? postgresExceptionMapper = null) : IUnitOfWork
    where TDbContext : DbContext
{
    public async ValueTask RunAsync(
        Func<IMediator, CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken)
    {
        await operation(mediator, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<TResult> RunAsync<TResult>(
        Func<IMediator, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var result = await operation(mediator, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return result;
    }

    private async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await dbContext.SaveChangesAsync(cancellationToken);

            if (result <= 0 || dbContext is not IOutboxDbContext outboxDbContext) return;

            // Best effort flush of the outbox messages to get fast dispatch.
            // Even if this fails, the messages are still in the outbox and will be retried later by the worker.
            var outboxDispatcher = new OutboxDispatcher(outboxDbContext, outboxMessageSender);
            if (await outboxDispatcher.DispatchTrackedAsync(cancellationToken))
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pge)
        {
            if (postgresExceptionMapper?.TryMap(pge, out var error) ?? false)
            {
                throw new BusinessRuleViolationException(error);
            }

            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleViolationException(Errors.ConcurrencyConflict());
        }
    }
    
    private static class Errors
    {
        public static Error ConcurrencyConflict() =>
            new(
                "concurrency_conflict",
                "The resource was modified by another operation.",
                ErrorType.Conflict);
    }
}