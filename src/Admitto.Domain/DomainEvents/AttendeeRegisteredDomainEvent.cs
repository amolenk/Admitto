namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeRegisteredDomainEvent(Guid TicketedEventId, Guid AttendeeId) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}