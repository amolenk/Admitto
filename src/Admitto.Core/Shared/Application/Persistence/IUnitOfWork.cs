namespace Amolenk.Admitto.Core.Shared.Application.Persistence;

public interface IUnitOfWork
{
    ValueTask SaveChangesAsync(
        CancellationToken cancellationToken = default,
        bool retryConcurrencyConflicts = false);
}
