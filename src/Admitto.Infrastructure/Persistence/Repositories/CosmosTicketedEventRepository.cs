using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.Exceptions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.Azure.Cosmos;

namespace Amolenk.Admitto.Infrastructure.Persistence.Repositories;

public class CosmosTicketedEventRepository(CosmosClient client) 
    : CosmosAggregateRepositoryBase<TicketedEvent>(client), ITicketedEventRepository
{
    public async ValueTask<IAggregateWithEtag<TicketedEvent>> GetByIdAsync(Guid id)
    {
        var result = await FindByIdAsync(id);
        if (result is null) throw new TicketedEventNotFoundException(id);

        return result;
    }
}
