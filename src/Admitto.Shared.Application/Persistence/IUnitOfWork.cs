using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Shared.Application.Persistence;

public interface IUnitOfWork
{
    ValueTask RunAsync(
        Func<IMediator, CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken);

    ValueTask<TResult> RunAsync<TResult>(
        Func<IMediator, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken);
}