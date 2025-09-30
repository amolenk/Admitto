using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record TeamMemberAddedDomainEvent(Guid TeamId, TeamMember Member) : DomainEvent;