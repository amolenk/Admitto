using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;

namespace Amolenk.Admitto.Infrastructure;

public class UnitOfWork(ApplicationContext context, MessageOutbox outbox) : IUnitOfWork
{
    private readonly List<Func<ValueTask>> _afterSaveCallbacks = [];

    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);

        // Execute and clear callbacks after changes are saved.
        foreach (var callback in _afterSaveCallbacks)
        {
            await callback();
        }
        _afterSaveCallbacks.Clear();
        
        // Flush the outbox to ensure all messages are sent.
        if (await outbox.FlushAsync(cancellationToken))
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public void RegisterAfterSaveCallback(Func<ValueTask> callback)
    {
        _afterSaveCallbacks.Add(callback);
    }
}