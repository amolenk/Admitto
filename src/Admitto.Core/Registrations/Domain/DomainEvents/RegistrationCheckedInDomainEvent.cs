using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record RegistrationCheckedInDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId RegistrationId,
    DateTimeOffset CheckedInAt) : DomainEvent;
