namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public interface IQueryHandler
{
}

public interface IQueryHandler<in TQuery, TResult> : IQueryHandler
    where TQuery : IQuery<TResult>
{
    ValueTask<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
