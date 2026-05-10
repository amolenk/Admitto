using System.Collections.Concurrent;
using System.Reflection;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventsInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>>
        _dispatchers = new();

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

    private ValueTask PublishDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var dispatcher = _dispatchers.GetOrAdd(eventType, static t =>
        {
            var method = typeof(DomainEventsInterceptor)
                .GetMethod(nameof(DispatchAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(t);
            return (Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>)
                Delegate.CreateDelegate(
                    typeof(Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>), method);
        });

        return dispatcher(serviceProvider, domainEvent, cancellationToken);
    }

    private static async ValueTask DispatchAsync<TDomainEvent>(
        IServiceProvider sp, IDomainEvent evt, CancellationToken ct)
        where TDomainEvent : IDomainEvent
    {
        var handlers = sp.GetServices<IDomainEventHandler<TDomainEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync((TDomainEvent)evt, ct);
    }
}
