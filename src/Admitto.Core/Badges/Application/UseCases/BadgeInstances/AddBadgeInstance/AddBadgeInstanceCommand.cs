using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.AddBadgeInstance;

internal sealed record AddBadgeInstanceCommand(
    Guid EventId,
    Guid TeamId,
    Guid BadgeTypeId,
    string DisplayName,
    string Notes) : Command<Guid>;
