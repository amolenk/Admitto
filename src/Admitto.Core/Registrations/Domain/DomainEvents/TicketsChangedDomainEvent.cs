using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

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
