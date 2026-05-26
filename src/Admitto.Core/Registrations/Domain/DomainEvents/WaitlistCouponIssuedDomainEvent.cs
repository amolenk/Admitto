using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record WaitlistCouponIssuedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId,
    EmailAddress RecipientEmail,
    CouponCode CouponCode,
    string TicketTypeName,
    DateTimeOffset ExpiresAt) : DomainEvent;
