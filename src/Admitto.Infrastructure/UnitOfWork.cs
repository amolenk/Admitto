using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;

namespace Amolenk.Admitto.Infrastructure;

public class UnitOfWork(ApplicationContext context, MessageOutbox outbox) : IUnitOfWork
{
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await context.SaveChangesAsync(cancellationToken);
        
        // Flush the outbox to ensure all messages are sent.
        if (result > 0 && await outbox.FlushAsync(cancellationToken))
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}