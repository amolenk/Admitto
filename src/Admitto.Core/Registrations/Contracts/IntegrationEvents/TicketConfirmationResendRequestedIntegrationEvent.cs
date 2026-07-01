using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

public sealed record TicketConfirmationResendRequestedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    Guid ResendRequestId,
    string RecipientEmail,
    string FirstName,
    string LastName,
    IReadOnlyList<string> TicketNames) : IntegrationEvent;
