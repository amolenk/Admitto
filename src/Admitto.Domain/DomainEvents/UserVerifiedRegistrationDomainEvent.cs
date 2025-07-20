using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record UserVerifiedRegistrationDomainEvent(Guid TicketedEventId, Guid RegistrationId,
    RegistrationType RegistrationType, IDictionary<string, int> Tickets)
    : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}