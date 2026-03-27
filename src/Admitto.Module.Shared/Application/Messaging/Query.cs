namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public interface IQuery<TResult>
{
    QueryId QueryId { get; }
}

public record Query<TResult> : IQuery<TResult>
{
    // Properties must be init-settable for deserialization.
    public QueryId QueryId { get; init; } = QueryId.New();
}