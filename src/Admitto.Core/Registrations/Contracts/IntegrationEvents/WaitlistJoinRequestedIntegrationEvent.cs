using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when an attendee requests to join a waitlist.
/// The Email module consumes this to send a signed verification link to the attendee.
/// </summary>
public sealed record WaitlistJoinRequestedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid TicketTypeId,
    string RecipientEmail,
    string VerificationToken) : IntegrationEvent;
