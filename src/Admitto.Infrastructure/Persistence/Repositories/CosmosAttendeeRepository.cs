using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.Exceptions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.Azure.Cosmos;

namespace Amolenk.Admitto.Infrastructure.Persistence.Repositories;

public class CosmosAttendeeRepository(CosmosClient client) 
    : CosmosAggregateRepositoryBase<Attendee>(client), IAttendeeRepository
{
    public async ValueTask<IAggregateWithEtag<Attendee>> GetByIdAsync(Guid id)
    {
        var result = await FindByIdAsync(id);
        if (result is null) throw new AttendeeNotFoundException();

        return result;
    }
}