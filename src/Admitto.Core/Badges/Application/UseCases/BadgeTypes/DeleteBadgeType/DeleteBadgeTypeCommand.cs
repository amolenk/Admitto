using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.DeleteBadgeType;

internal sealed record DeleteBadgeTypeCommand(Guid EventId, Guid TeamId, Guid BadgeTypeId) : Command;
