using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Domain.DomainEvents;

public record AttendeeRegisteredDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId RegistrationId,
    EmailAddress RecipientEmail,
    FirstName FirstName,
    LastName LastName,
    IReadOnlyList<TicketTypeSnapshot> Tickets) : DomainEvent;
