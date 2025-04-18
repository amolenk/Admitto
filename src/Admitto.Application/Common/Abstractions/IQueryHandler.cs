namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IQueryHandler
{
}

public interface IQueryHandler<in TQuery, TResultValue> : IQueryHandler
{
    ValueTask<Result<TResultValue>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

