namespace Amolenk.Admitto.Domain.DomainEvents;

public record TicketsReservationRejectedDomainEvent(Guid TicketedEventId, Guid RegistrationId) : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}