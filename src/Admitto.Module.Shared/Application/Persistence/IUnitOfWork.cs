namespace Amolenk.Admitto.Module.Shared.Application.Persistence;

public interface IUnitOfWork
{
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
}