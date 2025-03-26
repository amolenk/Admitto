using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record TeamMemberAddedDomainEvent(Guid TeamId, Guid UserId, string Email, UserRole Role) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}