using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.CreateBadgeEvent;

internal sealed record CreateBadgeEventCommand(Guid EventId, Guid TeamId) : Command;
