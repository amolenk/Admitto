using Amolenk.Admitto.Domain;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Data;

public static class EntityQueryExtensions
{
    public static async ValueTask<TEntity> GetEntityAsync<TEntity>(
        this IQueryable<TEntity> entities,
        Guid entityId,
        bool noTracking = false,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        if (noTracking)
        {
            entities = entities.AsNoTracking();
        }

        var entity = await entities
            .FirstOrDefaultAsync(r => r.Id == entityId, cancellationToken);

        if (entity is null)
        {
            throw new DomainRuleException(DomainRuleError.Entity.NotFound<TEntity>());
        }

        return entity;
    }
}