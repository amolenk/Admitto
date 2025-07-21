using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record TeamMemberAddedDomainEvent(Guid TeamId, string TeamSlug, TeamMember Member) : DomainEvent;