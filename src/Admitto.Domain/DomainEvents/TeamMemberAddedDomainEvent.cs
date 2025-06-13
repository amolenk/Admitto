using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record TeamMemberAddedDomainEvent(Guid TeamId, TeamMember Member) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}