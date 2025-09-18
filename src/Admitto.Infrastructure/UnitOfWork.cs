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

    public Action<UniqueViolationArgs>? OnUniqueViolation { get; set; }
    
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result;
        
        try
        {
            // TODO Retry DbUpdateConcurrencyException a few times with some delay
            
            result = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException pge && pge.SqlState == PostgresUniqueViolation)
        {
            var args = new UniqueViolationArgs
            {
                Error = GetSpecificUniqueViolationError(pge)
            };
            
            // Give the caller a chance to handle the unique violation.
            OnUniqueViolation?.Invoke(args);
            
            if (args.Retry)
            {
                // TODO
            }
            
            throw new ApplicationRuleException(args.Error);
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
    
    private static ApplicationRuleError GetSpecificUniqueViolationError(PostgresException ex)
    {
        return ex.TableName switch
        {
            "attendees" => ApplicationRuleError.Attendee.AlreadyRegistered,
            "contributors" => ApplicationRuleError.Contributor.AlreadyExists,
            "participants" => ApplicationRuleError.Participant.AlreadyExists,
            _ => ApplicationRuleError.General.AlreadyExists
        };
    }
}