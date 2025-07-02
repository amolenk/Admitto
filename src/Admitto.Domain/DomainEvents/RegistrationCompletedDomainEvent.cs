namespace Amolenk.Admitto.Domain.DomainEvents;

public class RegistrationCompletedDomainEvent : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}