using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.UpdateBadgeInstance;

internal sealed record UpdateBadgeInstanceCommand(
    Guid EventId,
    Guid TeamId,
    Guid BadgeTypeId,
    Guid BadgeInstanceId,
    string DisplayName,
    string Notes,
    uint? ExpectedVersion) : Command;
