using System.Linq.Expressions;
using Amolenk.Admitto.Core.Shared.Kernel.Abstractions;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Shared.Application.Persistence;

public static class DbSetExtensions
{
    extension<TEntity>(DbSet<TEntity> dbSet) where TEntity : class
    {
        public async ValueTask<TEntity> GetAsync(
            object key,
            CancellationToken cancellationToken = default)
        {
            var entity = await dbSet.FindAsync([key], cancellationToken);

            return entity ?? throw new BusinessRuleViolationException(NotFoundError.Create<TEntity>());
        }

        public async ValueTask<TEntity> GetAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var entity = await dbSet.FirstOrDefaultAsync(predicate, cancellationToken);

            return entity ?? throw new BusinessRuleViolationException(NotFoundError.Create<TEntity>());
        }

        public async ValueTask<TEntity> GetUntrackedAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var entity = await dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

            return entity ?? throw new BusinessRuleViolationException(NotFoundError.Create<TEntity>());
        }
    }

    extension<TEntity>(DbSet<TEntity> dbSet) where TEntity : class, IIsVersioned
    {
        public async ValueTask<TEntity> GetAsync(
            object key,
            uint? expectedVersion,
            CancellationToken cancellationToken = default)
        {
            var entity = await dbSet.GetAsync(key, cancellationToken);

            if (expectedVersion is null || expectedVersion == entity.Version)
            {
                return entity;
            }

            throw new BusinessRuleViolationException(
                ConcurrencyConflictError.Create(expectedVersion.Value, entity.Version));
        }

        public async ValueTask<TEntity> GetAsync(
            Expression<Func<TEntity, bool>> predicate,
            uint? expectedVersion,
            CancellationToken cancellationToken = default)
        {
            var entity = await dbSet.GetAsync(predicate, cancellationToken);

            if (expectedVersion is null || expectedVersion == entity.Version)
            {
                return entity;
            }

            throw new BusinessRuleViolationException(
                ConcurrencyConflictError.Create(expectedVersion.Value, entity.Version));
        }

        public async ValueTask<TEntity> GetUntrackedAsync(
            Expression<Func<TEntity, bool>> predicate,
            uint? expectedVersion,
            CancellationToken cancellationToken = default)
        {
            var entity = await dbSet.GetUntrackedAsync(predicate, cancellationToken);

            if (expectedVersion is null || expectedVersion == entity.Version)
            {
                return entity;
            }

            throw new BusinessRuleViolationException(
                ConcurrencyConflictError.Create(expectedVersion.Value, entity.Version));
        }
    }
}
