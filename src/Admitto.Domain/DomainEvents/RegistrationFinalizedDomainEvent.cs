namespace Amolenk.Admitto.Domain.DomainEvents;

public record RegistrationFinalizedDomainEvent(Guid AttendeeId, Guid RegistrationId) : IDomainEvent
{
    public Guid DomainEventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}