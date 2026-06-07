using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Badges.Domain.Entities;

/// <summary>
/// Aggregate root for badge management within a ticketed event.
/// Created when the event is created; transitions to Archived when the event is archived.
/// Owns badge types as a collection and enforces their lifecycle invariants.
/// </summary>
public sealed class BadgeEvent : Aggregate<TicketedEventId>
{
    private readonly List<BadgeType> _badgeTypes = [];

    private BadgeEvent() { }

    private BadgeEvent(TicketedEventId eventId, TeamId teamId, BadgeEventStatus status)
        : base(eventId)
    {
        TeamId = teamId;
        Status = status;
    }

    public TeamId TeamId { get; private set; }
    public BadgeEventStatus Status { get; private set; }
    public IReadOnlyList<BadgeType> BadgeTypes => _badgeTypes.AsReadOnly();

    public static BadgeEvent Create(TicketedEventId eventId, TeamId teamId)
        => new(eventId, teamId, BadgeEventStatus.Active);

    public void MarkArchived()
    {
        Status = BadgeEventStatus.Archived;
    }

    public void EnsureEventActive()
    {
        if (Status != BadgeEventStatus.Active)
            throw new BusinessRuleViolationException(Errors.EventNotActive);
    }

    public BadgeTypeId AddBadgeType(BadgeTypeName name, BadgeKind kind, IReadOnlyList<TicketTypeId> ticketTypeIds)
        => AddBadgeType(BadgeTypeId.New(), name, kind, ticketTypeIds);

    public BadgeTypeId AddBadgeType(
        BadgeTypeId badgeTypeId,
        BadgeTypeName name,
        BadgeKind kind,
        IReadOnlyList<TicketTypeId> ticketTypeIds)
    {
        EnsureEventActive();

        // Enforce name uniqueness (case-insensitive)
        if (_badgeTypes.Any(bt => bt.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException(Errors.BadgeTypeNameAlreadyExists);

        // Enforce ticket-based badge types have at least one ticket type
        if (kind == BadgeKind.TicketBased && ticketTypeIds.Count == 0)
            throw new BusinessRuleViolationException(Errors.TicketTypeIdsRequired);

        var badgeType = BadgeType.Create(badgeTypeId, name, kind, ticketTypeIds);
        _badgeTypes.Add(badgeType);
        return badgeTypeId;
    }

    public void RenameBadgeType(BadgeTypeId badgeTypeId, BadgeTypeName newName)
    {
        EnsureEventActive();

        var badgeType = _badgeTypes.FirstOrDefault(bt => bt.Id == badgeTypeId);
        if (badgeType is null)
            throw new BusinessRuleViolationException(Errors.BadgeTypeNotFound);

        // Enforce name uniqueness (case-insensitive), excluding the current badge type
        if (_badgeTypes.Any(bt => bt.Id != badgeTypeId && bt.Name.Value.Equals(newName.Value, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException(Errors.BadgeTypeNameAlreadyExists);

        badgeType.Rename(newName);
    }

    public BadgeKind DeleteBadgeType(BadgeTypeId badgeTypeId)
    {
        EnsureEventActive();

        var badgeType = _badgeTypes.FirstOrDefault(bt => bt.Id == badgeTypeId);
        if (badgeType is null)
            throw new BusinessRuleViolationException(Errors.BadgeTypeNotFound);

        var kind = badgeType.Kind;
        _badgeTypes.Remove(badgeType);
        return kind;
    }

    public void EnsureCanManageInstances(BadgeTypeId badgeTypeId)
    {
        EnsureEventActive();

        var badgeType = _badgeTypes.FirstOrDefault(bt => bt.Id == badgeTypeId);
        if (badgeType is null)
            throw new BusinessRuleViolationException(Errors.BadgeTypeNotFound);

        if (badgeType.Kind != BadgeKind.Standalone)
            throw new BusinessRuleViolationException(Errors.NotStandaloneBadgeType);
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "badges_event.event_not_active",
            "Badge types cannot be modified because the event is not active.",
            Type: ErrorType.Validation);

        public static readonly Error BadgeTypeNotFound = new(
            "badges_event.badge_type_not_found",
            "The specified badge type was not found.",
            Type: ErrorType.NotFound);

        public static readonly Error BadgeTypeNameAlreadyExists = new(
            "badges_event.badge_type_name_already_exists",
            "A badge type with this name already exists in this event.",
            Type: ErrorType.Conflict);

        public static readonly Error TicketTypeIdsRequired = new(
            "badges_event.ticket_type_ids_required",
            "A ticket-based badge type must reference at least one ticket type.",
            Type: ErrorType.Validation);

        public static readonly Error NotStandaloneBadgeType = new(
            "badges_event.not_standalone_badge_type",
            "This badge type is not a standalone badge type.",
            Type: ErrorType.Validation);
    }
}
