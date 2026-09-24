using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// Owns the ticket types for an event. Keyed by TicketedEventId.
/// Combines ticket type definition with capacity tracking in a single aggregate.
/// </summary>
public class TicketCatalog : Aggregate<TicketedEventId>
{
    private readonly List<TicketType> _ticketTypes = [];

    private TicketCatalog() { }

    private TicketCatalog(TicketedEventId id, TeamId teamId) : base(id) { TeamId = teamId; }

    public TeamId TeamId { get; private set; }

    public IReadOnlyList<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    /// <summary>
    /// Projection of the owning <see cref="TicketedEvent"/> lifecycle status. Kept in sync
    /// via the in-module <c>TicketedEventStatusChangedDomainEvent</c> handler so that the
    /// atomic capacity claim can refuse to run once the event has been archived,
    /// even if a registration handler's earlier policy check observed Active.
    /// Transitions are one-way: Active → Archived.
    /// </summary>
    public EventLifecycleStatus EventStatus { get; private set; } = EventLifecycleStatus.Active;

    public static TicketCatalog Create(TicketedEventId eventId, TeamId teamId) => new(eventId, teamId);

    /// <summary>
    /// Transitions <see cref="EventStatus"/> to <see cref="EventLifecycleStatus.Archived"/>.
    /// Idempotent when already Archived. Legal from Active.
    /// </summary>
    public void MarkEventArchived()
    {
        if (EventStatus == EventLifecycleStatus.Archived) return;

        EventStatus = EventLifecycleStatus.Archived;
    }

    public void AddTicketType(
        TicketTypeId id,
        TicketTypeName name,
        TimeSlot[] timeSlots,
        int? maxCapacity,
        bool selfServiceEnabled = true,
        bool waitlistEnabled = false,
        int claimWindowHours = 8,
        ReconfirmationEmailLimit? maxReconfirmationEmails = null,
        int reservedCapacity = 0)
    {
        EnsureEventActive();

        if (_ticketTypes.Any(tt => string.Equals(tt.Name.Value, name.Value, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypeName(name));

        if (waitlistEnabled && maxCapacity is null)
            throw new BusinessRuleViolationException(Errors.WaitlistRequiresBoundedCapacity(id));

        if (reservedCapacity < 0)
            throw new BusinessRuleViolationException(Errors.ReservedCapacityNegative(id));

        if (reservedCapacity > 0 && maxCapacity is null)
            throw new BusinessRuleViolationException(Errors.ReservedCapacityRequiresBoundedCapacity(id));

        if (maxCapacity is not null && reservedCapacity > maxCapacity.Value)
            throw new BusinessRuleViolationException(Errors.ReservedCapacityExceedsCapacity(id));

        _ticketTypes.Add(new TicketType(id, name, timeSlots, maxCapacity, selfServiceEnabled, waitlistEnabled, claimWindowHours, maxReconfirmationEmails, reservedCapacity));
        AddDomainEvent(new TicketCatalogSelfServiceTicketTypeCountChangedDomainEvent(
            TeamId,
            Id,
            Version,
            _ticketTypes.Count(t => t.SelfServiceEnabled)));

        // A newly added waitlist-enabled type can be immediately publicly sold out
        // when ReservedCapacity consumes the entire MaxCapacity.
        var added = _ticketTypes[^1];
        if (waitlistEnabled && added.IsSoldOut)
        {
            added.ActivateWaitlistMode();
            AddDomainEvent(new WaitlistModeActivatedDomainEvent(TeamId, Id, id));
        }
    }

    public void UpdateTicketType(
        TicketTypeId id,
        TicketTypeName? name,
        int? maxCapacity,
        bool? selfServiceEnabled = null,
        bool? waitlistEnabled = null,
        int? claimWindowHours = null,
        ReconfirmationEmailLimit? maxReconfirmationEmails = null,
        bool updateMaxReconfirmationEmails = false,
        int reservedCapacity = 0)
    {
        EnsureEventActive();

        if (reservedCapacity < 0)
            throw new BusinessRuleViolationException(Errors.ReservedCapacityNegative(id));

        if (reservedCapacity > 0 && maxCapacity is null)
            throw new BusinessRuleViolationException(Errors.ReservedCapacityRequiresBoundedCapacity(id));

        if (maxCapacity is not null && reservedCapacity > maxCapacity.Value)
            throw new BusinessRuleViolationException(Errors.ReservedCapacityExceedsCapacity(id));

        var previousSelfServiceCount = _ticketTypes.Count(t => t.SelfServiceEnabled);

        var ticketType = FindTicketType(id);

        if (name is not null)
            ticketType.UpdateName(name.Value);

        if (selfServiceEnabled is not null)
            ticketType.UpdateSelfServiceEnabled(selfServiceEnabled.Value);

        if (claimWindowHours is not null)
            ticketType.UpdateClaimWindowHours(claimWindowHours.Value);

        if (updateMaxReconfirmationEmails)
            ticketType.UpdateMaxReconfirmationEmails(maxReconfirmationEmails);

        // Disabling waitlist or removing capacity limit forces waitlist off
        bool forceDisabling = (waitlistEnabled == false && ticketType.WaitlistEnabled)
                              || (maxCapacity is null && ticketType.WaitlistEnabled);

        if (forceDisabling)
        {
            if (ticketType.WaitlistMode)
                ticketType.DeactivateWaitlistMode();
            ticketType.DisableWaitlist();
            AddDomainEvent(new WaitlistForcedDisabledDomainEvent(TeamId, Id, id));
            ticketType.UpdateCapacity(maxCapacity);
            ticketType.UpdateReservedCapacity(reservedCapacity);
            var forcedBranchSelfServiceCount = _ticketTypes.Count(t => t.SelfServiceEnabled);
            if (forcedBranchSelfServiceCount != previousSelfServiceCount)
            {
                AddDomainEvent(new TicketCatalogSelfServiceTicketTypeCountChangedDomainEvent(
                    TeamId,
                    Id,
                    Version,
                    forcedBranchSelfServiceCount));
            }
            return;
        }

        // Enabling waitlist
        if (waitlistEnabled == true && !ticketType.WaitlistEnabled)
        {
            var effectiveMaxCapacity = maxCapacity ?? ticketType.MaxCapacity;
            if (effectiveMaxCapacity is null)
                throw new BusinessRuleViolationException(Errors.WaitlistRequiresBoundedCapacity(id));

            ticketType.EnableWaitlist();
        }

        // Update capacity/reserved capacity and handle freed slots or retroactive activation
        var previousMaxCapacity = ticketType.MaxCapacity;
        var previousReservedCapacity = ticketType.ReservedCapacity;
        ticketType.UpdateCapacity(maxCapacity);
        ticketType.UpdateReservedCapacity(reservedCapacity);

        // Retroactive WaitlistMode activation: enabled on a sold-out type
        if (ticketType.WaitlistEnabled && !ticketType.WaitlistMode && ticketType.IsSoldOut)
        {
            ticketType.ActivateWaitlistMode();
            AddDomainEvent(new WaitlistModeActivatedDomainEvent(TeamId, Id, id));
        }
        // Public capacity increase while WaitlistMode active → notify waiting attendees.
        // "Available" is measured against the public threshold (MaxCapacity - ReservedCapacity),
        // so raising MaxCapacity or lowering ReservedCapacity can both free public slots.
        else if (ticketType.WaitlistMode && ticketType.MaxCapacity.HasValue)
        {
            var oldAvailable = ticketType.PublicAvailableCapacity(previousMaxCapacity, previousReservedCapacity);
            var newAvailable = ticketType.PublicAvailableCapacity(ticketType.MaxCapacity, ticketType.ReservedCapacity);
            var freedSlots = newAvailable - oldAvailable;
            if (freedSlots > 0)
                AddDomainEvent(new WaitlistCapacityFreedDomainEvent(TeamId, Id, id, freedSlots));
        }

        var currentSelfServiceCount = _ticketTypes.Count(t => t.SelfServiceEnabled);
        if (currentSelfServiceCount != previousSelfServiceCount)
        {
            AddDomainEvent(new TicketCatalogSelfServiceTicketTypeCountChangedDomainEvent(
                TeamId,
                Id,
                Version,
                currentSelfServiceCount));
        }
    }

    /// <summary>
    /// Re-evaluates WaitlistMode for the given ticket type. Clears WaitlistMode only when
    /// all three conditions hold: available capacity, no active waitlist entries, and no issued coupons.
    /// </summary>
    public void ReEvaluateWaitlistMode(TicketTypeId ticketTypeId, int activeEntryCount, int issuedCouponCount)
    {
        EnsureEventActive();

        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistMode) return;

        if (activeEntryCount == 0
            && issuedCouponCount == 0
            && !ticketType.IsSoldOut)
        {
            ticketType.DeactivateWaitlistMode();
        }
    }

    /// <summary>
    /// Clears WaitlistMode only when publicly available capacity exists (not IsSoldOut).
    /// Called when the Waitlist aggregate signals it is exhausted (no active entries, no issued coupons).
    /// </summary>
    public void TryDeactivateWaitlistMode(TicketTypeId ticketTypeId)
    {
        EnsureEventActive();

        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistMode) return;

        if (!ticketType.IsSoldOut)
        {
            ticketType.DeactivateWaitlistMode();
        }
    }

    /// <summary>
    /// Unconditionally clears WaitlistMode for a ticket type (used on admin force-disable).
    /// </summary>
    public void ForceDeactivateWaitlistMode(TicketTypeId ticketTypeId)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistMode) return;

        ticketType.DeactivateWaitlistMode();
    }

    public void EnsureEventActive()
    {
        if (EventStatus != EventLifecycleStatus.Active)
            throw new BusinessRuleViolationException(Errors.EventNotActive);
    }

    public TicketType? GetTicketType(TicketTypeId id)
    {
        return _ticketTypes.FirstOrDefault(tt => tt.Id == id);
    }

    /// <summary>
    /// Validates that the given ID selection has no duplicates, unknown IDs,
    /// or overlapping time slots. Does not modify capacity.
    /// Use this before delta-based claim/release operations to enforce invariants
    /// on the full new selection.
    /// </summary>
    public void ValidateSelection(IReadOnlyList<TicketTypeId> ids)
    {
        EnsureEventActive();

        if (ids.Count == 0) return;

        var ticketTypeMap = _ticketTypes.ToDictionary(t => t.Id);

        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key.Value).ToArray();
        if (duplicates.Length > 0)
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypes(duplicates));

        var unknownIds = ids.Where(id => !ticketTypeMap.ContainsKey(id)).Select(id => id.Value).ToArray();
        if (unknownIds.Length > 0)
            throw new BusinessRuleViolationException(Errors.UnknownTicketTypes(unknownIds));

        var allTimeSlots = ids
            .SelectMany(id => ticketTypeMap[id].TimeSlots.Select(ts => ts.Value))
            .ToList();
        var overlapping = allTimeSlots.GroupBy(ts => ts).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (overlapping.Length > 0)
            throw new BusinessRuleViolationException(Errors.OverlappingTimeSlots(overlapping));
    }

    /// <summary>
    /// Claims tickets for the given IDs. Validates the selection (duplicates,
    /// unknown IDs, self-service availability, overlapping time slots) before claiming capacity.
    /// <see cref="ClaimMode.Public"/> enforces capacity and requires self-service to be enabled.
    /// <see cref="ClaimMode.PublicUncapped"/> and <see cref="ClaimMode.Reserved"/> are uncapped
    /// (admin/coupon paths) — see <see cref="ClaimMode"/> for which pool each consumes.
    /// Returns snapshots of the claimed ticket types, tagged with the mode used so a later
    /// <see cref="Release"/> credits the correct pool back.
    /// </summary>
    public IReadOnlyList<TicketTypeSnapshot> Claim(IReadOnlyList<TicketTypeId> ids, ClaimMode mode)
    {
        EnsureEventActive();

        if (ids.Count == 0) return [];

        var ticketTypeMap = _ticketTypes.ToDictionary(t => t.Id);

        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key.Value).ToArray();
        if (duplicates.Length > 0)
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypes(duplicates));

        var unknownIds = ids.Where(id => !ticketTypeMap.ContainsKey(id)).Select(id => id.Value).ToArray();
        if (unknownIds.Length > 0)
            throw new BusinessRuleViolationException(Errors.UnknownTicketTypes(unknownIds));

        if (mode == ClaimMode.Public)
        {
            var nonSelfService = ids.Where(id => !ticketTypeMap[id].SelfServiceEnabled).Select(id => id.Value).ToArray();
            if (nonSelfService.Length > 0)
                throw new BusinessRuleViolationException(Errors.TicketTypesNotSelfService(nonSelfService));

        }

        var allTimeSlots = ids
            .SelectMany(id => ticketTypeMap[id].TimeSlots.Select(ts => ts.Value))
            .ToList();
        var overlapping = allTimeSlots.GroupBy(ts => ts).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (overlapping.Length > 0)
            throw new BusinessRuleViolationException(Errors.OverlappingTimeSlots(overlapping));

        foreach (var id in ids)
        {
            var ticketType = ticketTypeMap[id];
            ticketType.Claim(mode);

            // Activate WaitlistMode when the last public slot is claimed on a WaitlistEnabled type
            if (mode == ClaimMode.Public && ticketType.WaitlistEnabled && !ticketType.WaitlistMode && ticketType.IsSoldOut)
            {
                ticketType.ActivateWaitlistMode();
                AddDomainEvent(new WaitlistModeActivatedDomainEvent(TeamId, Id, id));
            }
        }

        return ids.Select(id =>
        {
            var ticketType = ticketTypeMap[id];
            return new TicketTypeSnapshot(id, ticketType.Name, ticketType.TimeSlots, mode);
        }).ToList();
    }

    /// <summary>
    /// Releases capacity for the given ticket snapshots. Unknown IDs are silently skipped.
    /// Each snapshot's <see cref="TicketTypeSnapshot.Mode"/> determines which counter is
    /// credited back (see <see cref="TicketType.ReleaseCapacity"/>). UsedCapacity is clamped at zero.
    /// </summary>
    public void Release(IReadOnlyList<TicketTypeSnapshot> tickets)
    {
        foreach (var ticket in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticket.Id);
            ticketType?.ReleaseCapacity(ticket.Mode);
        }
    }

    private TicketType FindTicketType(TicketTypeId id)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == id);
        if (ticketType is null)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotFound(id));

        return ticketType;
    }

    internal static class Errors
    {
        public static Error DuplicateTicketTypes(Guid[] ids) =>
            new("ticket_catalog.duplicate_ticket_types",
                "Duplicate ticket types in selection.",
                Details: new Dictionary<string, object?> { ["ids"] = ids });

        public static Error UnknownTicketTypes(Guid[] ids) =>
            new("ticket_catalog.unknown_ticket_types",
                "One or more ticket types do not exist.",
                Details: new Dictionary<string, object?> { ["ids"] = ids });

        public static Error TicketTypesNotSelfService(Guid[] ids) =>
            new("ticket_type.not_self_service",
                "One or more ticket types are not available for self-service registration.",
                Details: new Dictionary<string, object?> { ["ids"] = ids });

        public static Error TicketTypesNotAvailable(Guid[] ids) =>
            new("ticket_type.not_available",
                "One or more ticket types are not available for self-service registration.",
                Details: new Dictionary<string, object?> { ["ids"] = ids });

        public static Error OverlappingTimeSlots(string[] slots) =>
            new("ticket_catalog.overlapping_time_slots",
                "Selected ticket types have overlapping time slots.",
                Details: new Dictionary<string, object?> { ["slots"] = slots });

        public static Error DuplicateTicketTypeName(TicketTypeName name) =>
            new("ticket_catalog.duplicate_name",
                "A ticket type with this name already exists.",
                Details: new Dictionary<string, object?> { ["name"] = name.Value });

        public static Error TicketTypeNotFound(TicketTypeId id) =>
            new("ticket_catalog.ticket_type_not_found",
                "Ticket type could not be found.",
                Type: ErrorType.NotFound,
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error WaitlistRequiresBoundedCapacity(TicketTypeId id) =>
            new("ticket_catalog.waitlist_requires_bounded_capacity",
                "WaitlistEnabled requires a bounded capacity (MaxCapacity must be set).",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error ReservedCapacityRequiresBoundedCapacity(TicketTypeId id) =>
            new("ticket_catalog.reserved_capacity_requires_bounded_capacity",
                "ReservedCapacity requires a bounded capacity (MaxCapacity must be set).",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error ReservedCapacityNegative(TicketTypeId id) =>
            new("ticket_catalog.reserved_capacity_negative",
                "ReservedCapacity cannot be negative.",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error ReservedCapacityExceedsCapacity(TicketTypeId id) =>
            new("ticket_catalog.reserved_capacity_exceeds_capacity",
                "ReservedCapacity cannot exceed MaxCapacity.",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static readonly Error EventNotActive = new(
            "ticket_catalog.event_not_active",
            "Operation not allowed: the ticketed event is not Active.",
            Type: ErrorType.Validation);
    }
}
