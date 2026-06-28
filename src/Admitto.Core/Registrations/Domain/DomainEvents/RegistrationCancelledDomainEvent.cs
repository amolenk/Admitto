using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record RegistrationCancelledDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId RegistrationId,
    EmailAddress Email,
    FirstName FirstName,
    LastName LastName,
    CancellationReason Reason) : DomainEvent
{
    public RegistrationCancelledDomainEvent(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        RegistrationId registrationId,
        EmailAddress email,
        CancellationReason reason)
        : this(teamId, ticketedEventId, registrationId, email, FirstName.From("Unknown"), LastName.From("Attendee"), reason)
    {
    }
}
