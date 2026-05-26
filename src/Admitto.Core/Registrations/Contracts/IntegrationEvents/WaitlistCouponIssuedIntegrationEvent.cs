using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when a waitlist coupon is issued to a waiting attendee.
/// The Email module consumes this to send the notification email with coupon code and expiry.
/// </summary>
public sealed record WaitlistCouponIssuedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    string RecipientEmail,
    string CouponCode,
    string TicketTypeName,
    DateTimeOffset ExpiresAt) : IntegrationEvent;
