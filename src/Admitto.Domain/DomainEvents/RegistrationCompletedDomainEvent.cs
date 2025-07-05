namespace Amolenk.Admitto.Domain.DomainEvents;

public record RegistrationCompletedDomainEvent(Guid RegistrationId) : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}