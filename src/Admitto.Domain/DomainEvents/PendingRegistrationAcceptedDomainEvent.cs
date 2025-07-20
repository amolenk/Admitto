namespace Amolenk.Admitto.Domain.DomainEvents;

public record PendingRegistrationAcceptedDomainEvent(Guid TicketedEventId, Guid RegistrationRequestId) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}