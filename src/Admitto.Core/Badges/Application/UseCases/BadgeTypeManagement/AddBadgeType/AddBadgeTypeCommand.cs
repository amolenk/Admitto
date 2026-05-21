using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeTypeManagement.AddBadgeType;

internal sealed record AddBadgeTypeCommand(
    Guid EventId,
    string Name,
    string Kind,
    IReadOnlyList<Guid> TicketTypeIds) : Command<Guid>;
