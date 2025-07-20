namespace Amolenk.Admitto.Domain.DomainEvents;

public record PendingRegistrationRejectedDomainEvent(Guid TicketedEventId, Guid RegistrationRequestId) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}