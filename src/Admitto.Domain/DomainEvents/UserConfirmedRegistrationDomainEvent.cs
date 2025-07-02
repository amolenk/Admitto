namespace Amolenk.Admitto.Domain.DomainEvents;

public record UserConfirmedRegistrationDomainEvent(Guid TicketedEventId, Guid RegistrationId, IDictionary<Guid, int> Tickets)
    : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}