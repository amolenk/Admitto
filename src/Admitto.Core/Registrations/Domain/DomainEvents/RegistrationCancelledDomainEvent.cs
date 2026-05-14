using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record RegistrationCancelledDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId RegistrationId,
    EmailAddress Email,
    CancellationReason Reason) : DomainEvent;
