namespace Amolenk.Admitto.Domain.DomainEvents;

public record SpeakerAddedDomainEvent(Guid TicketedEventId, Guid SpeakerId) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}