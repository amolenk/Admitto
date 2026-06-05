using System.Collections.Concurrent;
using System.Reflection;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventsInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    /// <summary>
    /// Cache of domain event dispatchers to avoid recurring reflection overhead.
    /// </summary>
    private static readonly
        ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>>
        Dispatchers = new();

    /// <summary>
    /// When saving, dispatches all pending domain events to all registered handlers.
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null) return result;

        foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is not IDomainEventsProvider provider) continue;

            var events = provider.GetDomainEvents().ToArray();
            if (events.Length == 0) continue;

            foreach (var domainEvent in events)
            {
                await PublishDomainEventAsync(domainEvent, cancellationToken);
            }

            provider.ClearDomainEvents();
        }

        return result;
    }

    /// <summary>
    /// Dispatches a domain event to all registered handlers.
    /// </summary>
    private ValueTask PublishDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();

        // Get or add a dispatcher for the event type. The dispatcher is a compiled delegate that invokes the generic
        // CallDomainEventHandlersAsync method. The dispatcher is cached to avoid reflection overhead.
        var dispatcher = Dispatchers.GetOrAdd(
            eventType,
            static t =>
            {
                var method = typeof(DomainEventsInterceptor)
                    .GetMethod(nameof(CallDomainEventHandlersAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(t);
                return (Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>)
                    Delegate.CreateDelegate(
                        typeof(Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>),
                        method);
            });

        return dispatcher(serviceProvider, domainEvent, cancellationToken);
    }

    /// <summary>
    /// Calls all registered handlers for the given domain event.
    /// </summary>
    private static async ValueTask CallDomainEventHandlersAsync<TDomainEvent>(
        IServiceProvider serviceProvider,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
        where TDomainEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync((TDomainEvent)domainEvent, cancellationToken);
        }
    }
}
