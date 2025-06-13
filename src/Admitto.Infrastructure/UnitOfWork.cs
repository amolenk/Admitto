using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;

namespace Amolenk.Admitto.Infrastructure;

public class UnitOfWork(ApplicationContext context, MessageOutbox outbox) : IUnitOfWork
{
    public async ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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
}