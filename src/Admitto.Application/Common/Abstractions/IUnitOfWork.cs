namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IUnitOfWork
{
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
}