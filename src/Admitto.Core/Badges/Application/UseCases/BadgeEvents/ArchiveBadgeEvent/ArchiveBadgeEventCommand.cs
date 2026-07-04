using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.ArchiveBadgeEvent;

internal sealed record ArchiveBadgeEventCommand(Guid EventId) : Command;
