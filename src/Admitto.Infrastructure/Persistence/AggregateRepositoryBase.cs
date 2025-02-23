using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.Dtos;
using Amolenk.Admitto.Domain.Entities;
using MediatR;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public abstract class AggregateRepositoryBase<TAggregate>(IMediator mediator)
    where TAggregate : AggregateRoot
{
    private static readonly Dictionary<Guid, TAggregate> Aggregates = new();
    
    public ValueTask<AggregateResult<TAggregate>?> GetByIdAsync(Guid id)
    {
        if (Aggregates.TryGetValue(id, out var entity))
        {
            return ValueTask.FromResult<AggregateResult<TAggregate>?>(
                new AggregateResult<TAggregate>(entity, "etag"));
        }

        return ValueTask.FromResult<AggregateResult<TAggregate>?>(null);
    }
    
    public ValueTask<AggregateResult<TAggregate>> GetOrAddAsync(Guid id, Func<TAggregate> createAggregate)
    {
        if (Aggregates.TryGetValue(id, out var entity))
            return ValueTask.FromResult(new AggregateResult<TAggregate>(entity, "etag"));
        
        entity = createAggregate();
        Aggregates[id] = entity;

        return ValueTask.FromResult(new AggregateResult<TAggregate>(entity, "etag"));
    }

    public ValueTask SaveChangesAsync(TAggregate aggregate, string? etag = null, 
        IEnumerable<OutboxMessage>? outboxMessages = null, ICommand? processedCommand = null)
    {
        Aggregates[aggregate.Id] = aggregate;
        
        foreach (var outboxMessage in outboxMessages ?? [])
        {
            if (outboxMessage is CommandOutboxMessage commandOutboxMessage)
                mediator.Send(commandOutboxMessage.Command);
            else
                mediator.Publish(((DomainEventOutboxMessage)outboxMessage).DomainEvent);
        }
        
        aggregate.ClearDomainEvents();
        
        return ValueTask.CompletedTask;
    }
}
