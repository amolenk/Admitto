using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.RenameBadgeType;

internal sealed record RenameBadgeTypeCommand(Guid EventId, Guid BadgeTypeId, string Name) : Command;
