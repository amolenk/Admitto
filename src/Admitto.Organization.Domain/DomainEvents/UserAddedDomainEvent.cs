using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.DomainEvents;

public record UserAddedDomainEvent(
    UserId UserId,
    EmailAddress EmailAddress)
    : DomainEvent;
