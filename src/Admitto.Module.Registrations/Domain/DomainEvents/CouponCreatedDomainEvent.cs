using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;

public record CouponCreatedDomainEvent(
    CouponId CouponId,
    TicketedEventId TicketedEventId,
    EmailAddress Email) : DomainEvent;
