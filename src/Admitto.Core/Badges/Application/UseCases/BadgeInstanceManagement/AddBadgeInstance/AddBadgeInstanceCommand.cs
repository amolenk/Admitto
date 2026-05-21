using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.AddBadgeInstance;

internal sealed record AddBadgeInstanceCommand(
    Guid EventId,
    Guid BadgeTypeId,
    string DisplayName,
    string Notes) : Command<Guid>;
