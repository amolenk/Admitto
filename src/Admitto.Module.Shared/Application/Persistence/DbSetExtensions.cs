using Amolenk.Admitto.Module.Shared.Kernel.Abstractions;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Shared.Application.Persistence;

public static class DbSetExtensions
{
    public static async ValueTask<TEntity> GetAsync<TEntity, TKey>(
        this DbSet<TEntity> dbSet,
        TKey key,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TKey : notnull
    {
        var entity = await dbSet.FindAsync([key], cancellationToken);
        return entity ?? throw new BusinessRuleViolationException(NotFoundError.Create<TEntity>(key));
    }

    public static async ValueTask<TEntity> GetAsync<TEntity, TKey>(
        this DbSet<TEntity> dbSet,
        TKey key,
        uint? expectedVersion,
        CancellationToken cancellationToken = default)
        where TEntity : class, IIsVersioned
        where TKey : notnull
    {
        var entity = await dbSet.GetAsync(key, cancellationToken);

        if (expectedVersion is null || expectedVersion == entity.Version)
        {
            return entity;
        }

        throw new BusinessRuleViolationException(
            ConcurrencyConflictError.Create(expectedVersion.Value, entity.Version));
    }
}