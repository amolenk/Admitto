using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Data;

public static class EntityQueryExtensions
{
    public static async ValueTask<T> GetEntityAsync<T>(
        this IQueryable<T> entities,
        Guid entityId,
        bool noTracking = false,
        CancellationToken cancellationToken = default)
        where T : Entity
    {
        if (noTracking)
        {
            entities = entities.AsNoTracking();
        }

        var entity = await entities
            .FirstOrDefaultAsync(r => r.Id == entityId, cancellationToken);

        if (entity is null)
        {
            // TODO
            throw ValidationError.AttendeeRegistration.NotFound(entityId);
        }

        return entity;
    }
}