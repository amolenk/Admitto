namespace Amolenk.Admitto.Domain.DomainEvents;

public record RegistrationReceivedDomainEvent(Guid RegistrationId) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}