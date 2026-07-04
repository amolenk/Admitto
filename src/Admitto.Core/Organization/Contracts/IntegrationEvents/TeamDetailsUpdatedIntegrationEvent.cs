using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;

public sealed record TeamDetailsUpdatedIntegrationEvent(
    Guid TeamId,
    string Name,
    string AccentColor,
    string? ReplyToEmailAddress,
    uint TeamVersion) : IntegrationEvent;
