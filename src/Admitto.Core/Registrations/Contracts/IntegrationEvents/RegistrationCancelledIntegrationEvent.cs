using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when a registration is cancelled.
/// </summary>
public sealed record RegistrationCancelledIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string RecipientEmail,
    string Reason) : IntegrationEvent;
