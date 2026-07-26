using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Domain.DomainEvents;

public sealed record TeamCreatedDomainEvent(
    TeamId TeamId,
    TeamName Name,
    AccentColor AccentColor,
    uint TeamVersion) : DomainEvent;
