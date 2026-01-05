using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Infrastructure.Persistence.Interceptors;

public class DomainEventsInterceptor(IServiceProvider serviceProvider)
    : SaveChangesInterceptor
{
    private static readonly HashSet<Type> OutboxDomainEventTypes = LoadOutboxDomainEventTypes();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, 
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not ApplicationContext context) return result;

        var messageOutbox = serviceProvider.GetRequiredService<IMessageOutbox>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is not Aggregate aggregate) continue;
            
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
                if (OutboxDomainEventTypes.Contains(type))
                {
                    messageOutbox.Enqueue(domainEvent);
                }
            }
                
            aggregate.ClearDomainEvents();
        }

        return result;
    }
    
    private static HashSet<Type> LoadOutboxDomainEventTypes()
    {
        var applicationAssembly = typeof(IEventualDomainEventHandler<>).Assembly;
        var domainEventTypes = applicationAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventualDomainEventHandler<>))
                .Select(i => i.GetGenericArguments()[0]))
            .Distinct()
            .ToHashSet();

        return domainEventTypes;
    }
}