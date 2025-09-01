using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Amolenk.Admitto.Infrastructure;

public class UnitOfWork(ApplicationContext context, MessageOutbox outbox) : IUnitOfWork
{
    private const string PostgresUniqueViolation = "23505";

    public ApplicationRuleError? UniqueViolationError { get; set; }

    public async ValueTask SaveChangesAsync(
        Func<ValueTask>? onUniqueViolation = null,
        CancellationToken cancellationToken = default)
    {
        int result;
        
        try
        {
            // TODO Retry DbUpdateConcurrencyException a few times with some delay
            
            result = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresUniqueViolation })
        {
            if (onUniqueViolation is null)
            {
                var error = UniqueViolationError ?? ApplicationRuleError.General.AlreadyExists;
                throw new ApplicationRuleException(error);
            }
            
            await onUniqueViolation();
            return;
        }

        // Flush the outbox to ensure all messages are sent.
        if (result > 0 && await outbox.FlushAsync(cancellationToken))
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public void Clear()
    {
        context.ChangeTracker.Clear();
    }
}