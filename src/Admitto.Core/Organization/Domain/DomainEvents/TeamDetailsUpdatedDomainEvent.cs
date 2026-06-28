using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Domain.DomainEvents;

public sealed record TeamDetailsUpdatedDomainEvent(
    TeamId TeamId,
    TeamName Name,
    TeamAccentColor AccentColor,
    EmailAddress? ReplyToEmailAddress,
    uint TeamVersion) : DomainEvent;
