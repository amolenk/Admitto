using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Application.Exceptions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.Azure.Cosmos;

namespace Amolenk.Admitto.Infrastructure.Persistence.Repositories;

public class CosmosAttendeeRegistrationRepository(CosmosClient client) 
    : CosmosAggregateRepositoryBase<AttendeeRegistration>(client), IAttendeeRegistrationRepository
{
    public async ValueTask<IAggregateWithEtag<AttendeeRegistration>> GetByIdAsync(Guid id)
    {
        var result = await FindByIdAsync(id);
        if (result is null) throw new AttendeeNotFoundException();

        return result;
    }
}