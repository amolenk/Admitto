using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Badges.Domain.Entities;

/// <summary>
/// Defines a category of badge for a ticketed event.
/// Either ticket-based (one badge per matching registration) or standalone (manually managed instances).
/// </summary>
public sealed class BadgeType : Aggregate<BadgeTypeId>
{
    private BadgeType() { }

    private BadgeType(
        BadgeTypeId id,
        TicketedEventId eventId,
        BadgeTypeName name,
        BadgeKind kind,
        IReadOnlyList<TicketTypeId> ticketTypeIds)
        : base(id)
    {
        EventId = eventId;
        Name = name;
        Kind = kind;
        TicketTypeIds = ticketTypeIds;
    }

    public TicketedEventId EventId { get; private set; }
    public BadgeTypeName Name { get; private set; }
    public BadgeKind Kind { get; private set; }
    public IReadOnlyList<TicketTypeId> TicketTypeIds { get; private set; } = [];

    public static BadgeType Create(
        BadgeTypeId id,
        TicketedEventId eventId,
        BadgeTypeName name,
        BadgeKind kind,
        IReadOnlyList<TicketTypeId> ticketTypeIds)
    {
        if (kind == BadgeKind.TicketBased && ticketTypeIds.Count == 0)
            throw new BusinessRuleViolationException(Errors.TicketTypeIdsRequired);

        return new BadgeType(id, eventId, name, kind, ticketTypeIds);
    }

    public void Rename(BadgeTypeName newName)
    {
        Name = newName;
    }

    internal static class Errors
    {
        public static readonly Error TicketTypeIdsRequired = new(
            "badge_type.ticket_type_ids_required",
            "A ticket-based badge type must reference at least one ticket type.",
            Type: ErrorType.Validation);
    }
}
