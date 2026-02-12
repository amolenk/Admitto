using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.DomainEvents;

public sealed record UserCreatedDomainEvent(
    UserId UserId,
    EmailAddress EmailAddress)
    : DomainEvent;
