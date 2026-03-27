using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventsInterceptor(IServiceProvider serviceProvider, string moduleKey) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null) return result;

        // var serviceProvider = dbContext.GetInfrastructure();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Create an outbox writer if the DbContext supports outbox persistence.
        OutboxWriter? outboxWriter = null;
        if (dbContext is IOutboxDbContext outboxDbContext)
        {
            outboxWriter = new OutboxWriter(
                outboxDbContext, 
                messagePolicy: serviceProvider.GetRequiredKeyedService<IMessagePolicy>(moduleKey));
        }

        foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is not IDomainEventsProvider provider) continue;

            var events = provider.GetDomainEvents().ToArray();
            if (events.Length == 0) continue;
            
            foreach (var domainEvent in events)
            {
                // Publish domain events immediately so the handlers can run within the current transaction.
                await mediator.PublishDomainEventAsync(domainEvent, cancellationToken);

                // If an outbox writer is available, process the domain event for possible outbox persistence.
                // Exact behavior is determined by the message policy implemented in the module.
                outboxWriter?.TryEnqueue(domainEvent);
            }

            provider.ClearDomainEvents();
        }

        return result;
    }
}
