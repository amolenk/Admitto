using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.DeleteBadgeType;

internal sealed record DeleteBadgeTypeCommand(Guid EventId, Guid BadgeTypeId) : Command;
