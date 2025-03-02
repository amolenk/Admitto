using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.Exceptions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.Azure.Cosmos;

namespace Amolenk.Admitto.Infrastructure.Persistence;

// TODO Extract an interface for inheritors to implement

public abstract class CosmosAggregateRepositoryBase<TAggregate>(CosmosClient client)
    where TAggregate : AggregateRoot
{
    public async ValueTask<IAggregateWithEtag<TAggregate>?> FindByIdAsync(Guid id)
    {
        var container = GetContainer();

        try
        {
            var response = await container.ReadItemAsync<CosmosDocument<TAggregate>>(
                $"{typeof(TAggregate).Name}:{id}",
                new PartitionKey(id.ToString()));

            return new CosmosAggregateWithEtag<TAggregate>(response.Resource.Payload, response.ETag);
        }
        catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
    
    public async ValueTask<IAggregateWithEtag<TAggregate>> GetOrAddAsync(Guid id, Func<TAggregate> createAggregate)
    {
        var result = await FindByIdAsync(id);
        if (result is not null)
        {
            return result;
        }

        try
        {
            await SaveChangesAsync(createAggregate());
        }
        catch (Exception)
        {
            // 
        }

        return (await FindByIdAsync(id))!;
    }
    
    public async ValueTask SaveChangesAsync(TAggregate aggregate, string? etag = null, IEnumerable<OutboxMessage>? outboxMessages = null,
        ICommand? processedCommand = null)
    {
        var outboxMessageList = (outboxMessages ?? []).ToList();
        if (outboxMessageList.Count > 0)
        {
            await SaveBatchChangesAsync(aggregate, etag, outboxMessages, processedCommand);
        }
        else
        {
            await SaveSingleChangeAsync(aggregate, etag);
        }
    }
    
    private async ValueTask SaveSingleChangeAsync(TAggregate aggregate, string? etag = null)
    {
        var container = GetContainer();
        var document = new CosmosDocument<TAggregate>
        {
            Id = $"{typeof(TAggregate).Name}:{aggregate.Id}",
            PartitionKey = aggregate.Id.ToString(),
            Discriminator = typeof(TAggregate).Name,
            Payload = aggregate,
        };

        // TODO Decide on PK strategy: get from doc or repo?
        
        if (etag is null)
        {
            await container.CreateItemAsync(
                item: document,
                partitionKey: new PartitionKey(document.PartitionKey));
        }
        else
        {
            await container.ReplaceItemAsync(
                document,
                document.Id,
                partitionKey: new PartitionKey(document.PartitionKey),
                requestOptions: new ItemRequestOptions
                {
                    IfMatchEtag = etag
                });
        }
    }
    
    public async ValueTask DeleteAsync(Guid registrationId)
    {
        var container = GetContainer();
        await container.DeleteItemAsync<CosmosDocument<TAggregate>>(
            registrationId.ToString(),
            new PartitionKey(registrationId.ToString()));
    }
    
    private async ValueTask SaveBatchChangesAsync(TAggregate aggregate, string? etag = null, IEnumerable<OutboxMessage>? outboxMessages = null,
        ICommand? processedCommand = null)
    {
        var container = GetContainer();
        var partitionKey = aggregate.Id.ToString();
        var document = new CosmosDocument<TAggregate>
        {
            Id = $"{typeof(TAggregate).Name}:{aggregate.Id}",
            PartitionKey = partitionKey,
            Discriminator = typeof(TAggregate).Name,
            Payload = aggregate,
        };

        var batch = container.CreateTransactionalBatch(new PartitionKey(partitionKey));
        
        if (etag is null)
        {
            batch.CreateItem(document);
        }
        else
        {
            batch.ReplaceItem(document.Id, document, new TransactionalBatchItemRequestOptions
                {
                    IfMatchEtag = etag
                });
        }
        
        outboxMessages ??= [];
        foreach (var outboxMessage in outboxMessages)
        {
            var outboxDocument = new CosmosDocument<OutboxMessage>
            {
                Id = $"{nameof(OutboxMessage)}:{outboxMessage.Id}",
                PartitionKey = partitionKey,
                Discriminator = nameof(OutboxMessage),
                Payload = outboxMessage
            };

            batch.CreateItem(outboxDocument);
        }

        var response = await batch.ExecuteAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new ConcurrencyException("Failed to save changes.");
        }
    }
    
    private Container GetContainer()
    {
        var database = client.GetDatabase("admitto");
        var container = database.GetContainer("core");

        return container;
    }
}
