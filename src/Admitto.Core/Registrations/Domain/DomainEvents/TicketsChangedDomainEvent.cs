using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

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
    IReadOnlyList<TicketTypeSnapshot> OldWaitlistedTickets,
    IReadOnlyList<TicketTypeSnapshot> NewWaitlistedTickets,
    DateTimeOffset ChangedAt) : DomainEvent;
