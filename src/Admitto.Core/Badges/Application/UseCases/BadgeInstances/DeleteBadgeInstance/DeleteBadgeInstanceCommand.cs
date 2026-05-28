using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.DeleteBadgeInstance;

internal sealed record DeleteBadgeInstanceCommand(Guid EventId, Guid TeamId, Guid BadgeTypeId, Guid BadgeInstanceId) : Command;
