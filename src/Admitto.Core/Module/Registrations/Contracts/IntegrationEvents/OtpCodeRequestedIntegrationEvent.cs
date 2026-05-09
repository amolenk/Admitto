using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when an attendee requests an OTP code for email verification.
/// The Email module consumes this to send the OTP code to the attendee.
/// </summary>
public sealed record OtpCodeRequestedIntegrationEvent(
    Guid OtpCodeId,
    Guid TeamId,
    Guid TicketedEventId,
    string EventName,
    string RecipientEmail,
    string PlainCode) : IntegrationEvent;
