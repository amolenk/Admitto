using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;

public record OtpCodeRequestedDomainEvent(
    OtpCodeId OtpCodeId,
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string EventName,
    EmailAddress RecipientEmail,
    string PlainCode) : DomainEvent;
