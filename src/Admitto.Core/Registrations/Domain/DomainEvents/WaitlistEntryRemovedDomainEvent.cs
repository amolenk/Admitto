using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record WaitlistEntryRemovedDomainEvent(
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId,
    WaitlistEntryId EntryId,
    EmailAddress Email) : DomainEvent;
