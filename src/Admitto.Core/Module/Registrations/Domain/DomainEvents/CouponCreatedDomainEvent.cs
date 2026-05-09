using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Domain.DomainEvents;

public record CouponCreatedDomainEvent(
    CouponId CouponId,
    TicketedEventId TicketedEventId,
    EmailAddress Email) : DomainEvent;
