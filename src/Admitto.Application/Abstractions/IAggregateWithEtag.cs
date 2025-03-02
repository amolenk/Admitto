using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Abstractions;

public interface IAggregateWithEtag<TAggregate> where TAggregate : AggregateRoot
{
    public TAggregate Aggregate { get; }
    
    public string? ETag { get; }

    void Deconstruct(out TAggregate aggregate, out string? etag);

}