using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventsInterceptor(IServiceProvider serviceProvider, string moduleKey) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null) return result;

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is not IDomainEventsProvider provider) continue;

            var events = provider.GetDomainEvents().ToArray();
            if (events.Length == 0) continue;
            
            foreach (var domainEvent in events)
            {
                // Publish domain events immediately so the handlers can run within the current transaction.
                // Handlers that need deferred/cross-module delivery inject IOutbox and call Enqueue().
                await mediator.PublishDomainEventAsync(domainEvent, cancellationToken);
            }

            provider.ClearDomainEvents();
        }

        return result;
    }
}
