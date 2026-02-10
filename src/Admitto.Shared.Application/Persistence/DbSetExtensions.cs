using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Shared.Application.Persistence;

public static class DbSetExtensions
{
    public static async ValueTask<TEntity> GetAsync<TEntity, TKey>(
        this DbSet<TEntity> dbSet,
        TKey key,
        CancellationToken cancellationToken) where TEntity : class where TKey : struct
    {
        var entity = await dbSet.FindAsync([key], cancellationToken);
        if (entity is null)
        {
            throw new BusinessRuleViolationException(
                Errors.EntityNotFound(typeof(TEntity).Name, key.ToString() ?? string.Empty));
        }

        return entity;
    }

    private static class Errors
    {
        public static Error EntityNotFound(string entityName, string entityKey) => new(
            "not_found",
            "Entity not found.",
            Details: new Dictionary<string, object?>
            {
                ["entityName"] = entityName,
                ["entityKey"] = entityKey
            });
    }
}