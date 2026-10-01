using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised when a waitlist offer lapses unclaimed. <see cref="RegistrationClosed"/> is set when it lapsed after the
/// registration window closed, so the recipient isn't invited to register again.
/// </summary>
public record WaitlistCouponExpiredDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId,
    EmailAddress RecipientEmail,
    CouponCode CouponCode,
    string TicketTypeName,
    bool RegistrationClosed) : DomainEvent;
