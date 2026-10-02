using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when a registration is cancelled.
/// <see cref="WasWaitlisted"/> is true when the registration held no confirmed tickets at the
/// moment of cancellation (i.e. the attendee was only on one or more waitlists).
/// </summary>
[method: JsonConstructor]
public sealed record RegistrationCancelledIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string RecipientEmail,
    string FirstName,
    string LastName,
    string Reason,
    bool WasWaitlisted) : IntegrationEvent;
