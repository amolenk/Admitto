using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypes.AddBadgeType;

internal sealed record AddBadgeTypeCommand(
    Guid EventId,
    Guid TeamId,
    string Name,
    string Kind,
    IReadOnlyList<Guid> TicketTypeIds) : Command<Guid>;
