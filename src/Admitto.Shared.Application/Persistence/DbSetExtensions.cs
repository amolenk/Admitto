using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Shared.Application.Persistence;

public static class DbSetExtensions
{
    public static async ValueTask<TEntity> GetAsync<TEntity, TKey>(
        this DbSet<TEntity> dbSet,
        TKey key,
        CancellationToken cancellationToken = default) where TEntity : class where TKey : notnull
    {
        var entity = await dbSet.FindAsync([key], cancellationToken);
        return entity ?? throw new BusinessRuleViolationException(NotFoundError.Create<TEntity>(key));
    }
}