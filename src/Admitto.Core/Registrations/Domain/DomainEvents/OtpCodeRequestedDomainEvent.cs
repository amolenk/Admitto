using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record OtpCodeRequestedDomainEvent(
    OtpCodeId OtpCodeId,
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    EventName EventName,
    EmailAddress RecipientEmail,
    string PlainCode) : DomainEvent;
