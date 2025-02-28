namespace Amolenk.Admitto.Domain.DomainEvents;

public record RegistrationAcceptedDomainEvent(Guid AttendeeId, Guid RegistrationId) : IDomainEvent
{
    public Guid DomainEventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}