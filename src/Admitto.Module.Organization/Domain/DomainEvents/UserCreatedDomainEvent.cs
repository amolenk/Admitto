using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.DomainEvents;

public sealed record UserCreatedDomainEvent(
    UserId UserId,
    EmailAddress EmailAddress)
    : DomainEvent;
