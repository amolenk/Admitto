using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record TeamMemberAddedDomainEvent(string TeamSlug, TeamMember Member) : DomainEvent;