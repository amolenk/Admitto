using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class DomainEventsInterceptor(IServiceProvider serviceProvider)
    : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, 
        InterceptionResult<int> result, CancellationToken cancellationToken = new CancellationToken())
    {
        if (eventData.Context is not ApplicationContext context) return result;

        var messageOutbox = serviceProvider.GetRequiredService<IMessageOutbox>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is not AggregateRoot aggregate) continue;
            
            foreach (var domainEvent in aggregate.GetDomainEvents())
            {
                var type = domainEvent.GetType();
                
                // Run transactional domain event handlers immediately.
                var transactionalHandlerType = typeof(ITransactionalDomainEventHandler<>).MakeGenericType(type);
                var handlers = serviceProvider.GetServices(transactionalHandlerType);
                foreach (dynamic? handler in handlers)
                {
                    if (handler is null) continue;
                    
                    await handler.HandleAsync((dynamic)domainEvent, cancellationToken);
                }

                // Put the event in the outbox if there are any eventual domain event handlers.
                var eventualHandlerType = typeof(IEventualDomainEventHandler<>).MakeGenericType(type);
                if (!serviceProvider.GetServices(eventualHandlerType).Any(h => h is not null)) continue;
                
                messageOutbox.Enqueue(domainEvent);
            }
                
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}