using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;

public sealed record ReconfirmAutoExpiredIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    IReadOnlyCollection<Guid> RegistrationIds) : IntegrationEvent;
