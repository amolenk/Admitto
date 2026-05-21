using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.UpdateBadgeInstance;

internal sealed record UpdateBadgeInstanceCommand(
    Guid EventId,
    Guid BadgeTypeId,
    Guid BadgeInstanceId,
    string DisplayName,
    string Notes) : Command;
