using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

public record WaitlistCouponExpiredDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    TicketTypeId TicketTypeId,
    EmailAddress RecipientEmail,
    CouponCode CouponCode,
    string TicketTypeName) : DomainEvent;
