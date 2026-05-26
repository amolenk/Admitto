using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when an organiser-created coupon is issued.
/// The Email module consumes this to send an invitation email to the target recipient.
/// Waitlist coupons do not publish this event — their notification is handled by the
/// waitlist notification flow instead.
/// </summary>
public sealed record CouponCreatedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    string RecipientEmail,
    string CouponCode) : IntegrationEvent;
