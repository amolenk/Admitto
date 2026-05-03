using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;

public record TicketsChangedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId RegistrationId,
    EmailAddress RecipientEmail,
    FirstName FirstName,
    LastName LastName,
    IReadOnlyList<TicketTypeSnapshot> OldTickets,
    IReadOnlyList<TicketTypeSnapshot> NewTickets,
    DateTimeOffset ChangedAt) : DomainEvent;
