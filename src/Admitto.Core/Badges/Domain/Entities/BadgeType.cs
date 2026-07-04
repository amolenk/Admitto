using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Badges.Domain.Entities;

/// <summary>
/// A plain domain class representing a category of badge for a ticketed event.
/// Either ticket-based (one badge per matching registration) or standalone (manually managed instances).
/// Owned by <see cref="BadgeEvent"/> aggregate.
/// </summary>
public sealed class BadgeType
{
    private BadgeType() { }

    private BadgeType(
        BadgeTypeId id,
        BadgeTypeName name,
        BadgeKind kind,
        IReadOnlyList<TicketTypeId> ticketTypeIds)
    {
        Id = id;
        Name = name;
        Kind = kind;
        TicketTypeIds = ticketTypeIds;
    }

    public BadgeTypeId Id { get; private set; }
    public BadgeTypeName Name { get; private set; }
    public BadgeKind Kind { get; private set; }
    public IReadOnlyList<TicketTypeId> TicketTypeIds { get; private set; } = [];

    public static BadgeType Create(
        BadgeTypeId id,
        BadgeTypeName name,
        BadgeKind kind,
        IReadOnlyList<TicketTypeId> ticketTypeIds)
    {
        if (kind == BadgeKind.TicketBased && ticketTypeIds.Count == 0)
            throw new BusinessRuleViolationException(Errors.TicketTypeIdsRequired);

        return new BadgeType(id, name, kind, ticketTypeIds);
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
