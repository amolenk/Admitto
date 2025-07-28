using System.ComponentModel.DataAnnotations;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Domain.Entities;

public abstract class AggregateRoot : Entity, IAuditable, IHasConcurrencyToken
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected AggregateRoot()
    {
    }
    
    protected AggregateRoot(Guid id) : base(id)
    {
    }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastChangedAt { get; set; }
    
    public string? LastChangedBy { get; set; }

    [Timestamp]
    public uint Version { get; set; }

    public IReadOnlyCollection<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}