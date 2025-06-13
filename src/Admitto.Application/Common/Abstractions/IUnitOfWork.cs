namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IUnitOfWork
{
    ValueTask<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}