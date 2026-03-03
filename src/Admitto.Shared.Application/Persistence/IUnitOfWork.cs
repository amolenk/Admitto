namespace Amolenk.Admitto.Shared.Application.Persistence;

public interface IUnitOfWork
{
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
}