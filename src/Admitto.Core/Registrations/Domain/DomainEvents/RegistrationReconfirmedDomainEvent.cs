using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record RegistrationReconfirmedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId RegistrationId,
    EmailAddress Email,
    DateTimeOffset ReconfirmedAt) : DomainEvent;
