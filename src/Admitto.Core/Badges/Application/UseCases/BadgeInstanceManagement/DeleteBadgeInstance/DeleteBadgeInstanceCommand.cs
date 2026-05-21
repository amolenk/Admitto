using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstanceManagement.DeleteBadgeInstance;

internal sealed record DeleteBadgeInstanceCommand(Guid EventId, Guid BadgeTypeId, Guid BadgeInstanceId) : Command;
