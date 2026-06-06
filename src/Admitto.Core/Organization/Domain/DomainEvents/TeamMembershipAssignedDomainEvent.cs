using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Organization.Domain.DomainEvents;

public sealed record TeamMembershipAssignedDomainEvent(
    UserId UserId,
    TeamId TeamId,
    TeamMembershipRole Role)
    : DomainEvent;
