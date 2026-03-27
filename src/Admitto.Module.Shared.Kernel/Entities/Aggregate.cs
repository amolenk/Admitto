using System.ComponentModel.DataAnnotations;
using Amolenk.Admitto.Module.Shared.Kernel.Abstractions;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Shared.Kernel.Entities;

public abstract class Aggregate<TId> : Entity<TId>, IIsAuditable, IIsVersioned, IDomainEventsProvider
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Aggregate()
    {
    }
    
    protected Aggregate(TId id) : base(id)
    {
    }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastChangedAt { get; set; }

    public EmailAddress LastChangedBy { get; set; }

    [Timestamp]
    public uint Version { get; set; }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}