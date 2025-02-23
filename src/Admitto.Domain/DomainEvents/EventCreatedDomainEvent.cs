namespace Amolenk.Admitto.Domain.DomainEvents;

public class EventCreatedDomainEvent(Guid eventId, string name, DateTime date) : IDomainEvent
{
    public Guid DomainEventId { get; } = eventId;
    public string Name { get; } = name;
    public DateTime Date { get; } = date;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}