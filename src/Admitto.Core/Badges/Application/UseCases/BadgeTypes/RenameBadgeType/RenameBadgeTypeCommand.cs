using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.RenameBadgeType;

internal sealed record RenameBadgeTypeCommand(Guid EventId, Guid TeamId, Guid BadgeTypeId, string Name, uint? ExpectedVersion) : Command;
