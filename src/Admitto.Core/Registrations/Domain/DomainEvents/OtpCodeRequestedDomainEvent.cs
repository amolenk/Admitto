using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record OtpCodeRequestedDomainEvent(
    OtpCodeId OtpCodeId,
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string EventName,
    EmailAddress RecipientEmail,
    string PlainCode) : DomainEvent;
