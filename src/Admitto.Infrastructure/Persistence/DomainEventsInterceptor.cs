using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class DomainEventsInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, 
        InterceptionResult<int> result, CancellationToken cancellationToken = new CancellationToken())
    {
        if (eventData.Context is not ApplicationContext context) return result;
        
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not AggregateRoot aggregate) continue;
            
            foreach (var domainEvent in aggregate.GetDomainEvents())
            {
                var type = domainEvent.GetType();
                
                var immediateHandlerType = typeof(IImmediateDomainEventHandler<>).MakeGenericType(type);
                var handlers = serviceProvider.GetServices(immediateHandlerType);
                foreach (dynamic? handler in handlers)
                {
                    if (handler is null) continue;
                    
                    await handler.HandleAsync((dynamic)domainEvent, cancellationToken);
                }

                var eventualHandlerType = typeof(IDomainEventHandler<>).MakeGenericType(type);
                if (serviceProvider.GetServices(eventualHandlerType).Any(h => h is not null))
                {
                    context.Outbox.Add(OutboxMessage.FromDomainEvent(domainEvent));
                }
            }
                
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}