using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;

public sealed record TeamCreatedIntegrationEvent(
    Guid TeamId,
    string Name,
    string AccentColor,
    uint TeamVersion) : IntegrationEvent;
