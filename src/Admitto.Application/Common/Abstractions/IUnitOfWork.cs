using System.Linq.Expressions;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IUnitOfWork
{
    // TODO Don't need this after all
    void MarkAsModified<TEntity, TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class;

    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
    
    void RegisterAfterSaveCallback(Func<ValueTask> callback);
}