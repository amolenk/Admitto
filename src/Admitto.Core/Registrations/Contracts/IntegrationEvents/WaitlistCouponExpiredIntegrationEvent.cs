using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when a waitlist coupon lapses unclaimed and the queue
/// advances past its recipient. The Email module consumes this to tell the attendee their offer expired.
/// </summary>
public sealed record WaitlistCouponExpiredIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    string RecipientEmail,
    string CouponCode,
    string TicketTypeName) : IntegrationEvent;
