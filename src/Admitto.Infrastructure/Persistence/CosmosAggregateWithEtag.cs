using Amolenk.Admitto.Application.Abstractions;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class CosmosAggregateWithEtag<TAggregate>(TAggregate aggregate, string etag) : IAggregateWithEtag<TAggregate>
    where TAggregate : AggregateRoot
{
    public TAggregate Aggregate { get; } = aggregate;
    public string ETag { get; } = etag;

    public void Deconstruct(out TAggregate aggregate, out string? etag)
    {
        aggregate = Aggregate;
        etag = ETag;
    }
}
