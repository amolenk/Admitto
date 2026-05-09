using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when an attendee reconfirms attendance.
/// </summary>
public sealed record RegistrationReconfirmedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string RecipientEmail,
    DateTimeOffset ReconfirmedAt) : IntegrationEvent;
