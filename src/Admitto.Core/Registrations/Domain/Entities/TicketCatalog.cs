using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// Owns the ticket types for an event. Keyed by TicketedEventId.
/// Combines ticket type definition with capacity tracking in a single aggregate.
/// </summary>
public class TicketCatalog : Aggregate<TicketedEventId>
{
    private readonly List<TicketType> _ticketTypes = [];

    private TicketCatalog() { }

    private TicketCatalog(TicketedEventId id) : base(id) { }

    public IReadOnlyList<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    /// <summary>
    /// Projection of the owning <see cref="TicketedEvent"/> lifecycle status. Kept in sync
    /// via the in-module <c>TicketedEventStatusChangedDomainEvent</c> handler so that the
    /// atomic capacity claim can refuse to run once the event has been archived,
    /// even if a registration handler's earlier policy check observed Active.
    /// Transitions are one-way: Active → Archived.
    /// </summary>
    public EventLifecycleStatus EventStatus { get; private set; } = EventLifecycleStatus.Active;

    public static TicketCatalog Create(TicketedEventId eventId) => new(eventId);

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
        int? maxReconfirmAttempts = null)
    {
        EnsureEventActive();

        if (_ticketTypes.Any(tt => string.Equals(tt.Name.Value, name.Value, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypeName(name));

        if (waitlistEnabled && maxCapacity is null)
            throw new BusinessRuleViolationException(Errors.WaitlistRequiresBoundedCapacity(id));

        _ticketTypes.Add(new TicketType(id, name, timeSlots, maxCapacity, selfServiceEnabled, waitlistEnabled, claimWindowHours, maxReconfirmAttempts));
    }

    public void UpdateTicketType(
        TicketTypeId id,
        TicketTypeName? name,
        int? maxCapacity,
        bool? selfServiceEnabled = null,
        bool? waitlistEnabled = null,
        int? claimWindowHours = null,
        int? maxReconfirmAttempts = null,
        bool updateMaxReconfirmAttempts = false)
    {
        EnsureEventActive();

        var ticketType = FindTicketType(id);

        if (name is not null)
            ticketType.UpdateName(name.Value);

        if (selfServiceEnabled is not null)
            ticketType.UpdateSelfServiceEnabled(selfServiceEnabled.Value);

        if (claimWindowHours is not null)
            ticketType.UpdateClaimWindowHours(claimWindowHours.Value);

        if (updateMaxReconfirmAttempts)
            ticketType.UpdateMaxReconfirmAttempts(maxReconfirmAttempts);

        // Disabling waitlist or removing capacity limit forces waitlist off
        bool forceDisabling = (waitlistEnabled == false && ticketType.WaitlistEnabled)
                              || (maxCapacity is null && ticketType.WaitlistEnabled);

        if (forceDisabling)
        {
            if (ticketType.WaitlistMode)
                ticketType.DeactivateWaitlistMode();
            ticketType.DisableWaitlist();
            AddDomainEvent(new WaitlistForcedDisabledDomainEvent(Id, id));
            ticketType.UpdateCapacity(maxCapacity);
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

        // Update capacity and handle freed slots or retroactive activation
        var previousMaxCapacity = ticketType.MaxCapacity;
        ticketType.UpdateCapacity(maxCapacity);

        // Retroactive WaitlistMode activation: enabled on a sold-out type
        if (ticketType.WaitlistEnabled && !ticketType.WaitlistMode
            && ticketType.MaxCapacity.HasValue
            && ticketType.UsedCapacity >= ticketType.MaxCapacity.Value)
        {
            ticketType.ActivateWaitlistMode();
            AddDomainEvent(new WaitlistModeActivatedDomainEvent(Id, id));
        }
        // Capacity increase while WaitlistMode active → notify waiting attendees
        else if (ticketType.WaitlistMode && ticketType.MaxCapacity.HasValue)
        {
            var oldAvailable = Math.Max(0, (previousMaxCapacity ?? 0) - ticketType.UsedCapacity);
            var newAvailable = Math.Max(0, ticketType.MaxCapacity.Value - ticketType.UsedCapacity);
            var freedSlots = newAvailable - oldAvailable;
            if (freedSlots > 0)
                AddDomainEvent(new WaitlistCapacityFreedDomainEvent(Id, id, freedSlots));
        }
    }

    /// <summary>
    /// Re-evaluates WaitlistMode for the given ticket type. Clears WaitlistMode only when
    /// all three conditions hold: available capacity, no active waitlist entries, and no issued coupons.
    /// </summary>
    public void ReEvaluateWaitlistMode(TicketTypeId ticketTypeId, int activeEntryCount, int issuedCouponCount)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistMode) return;

        if (ticketType.MaxCapacity.HasValue
            && ticketType.UsedCapacity < ticketType.MaxCapacity.Value
            && activeEntryCount == 0
            && issuedCouponCount == 0)
        {
            ticketType.DeactivateWaitlistMode();
        }
    }

    /// <summary>
    /// Clears WaitlistMode only when capacity is available (UsedCapacity &lt; MaxCapacity).
    /// Called when the Waitlist aggregate signals it is exhausted (no active entries, no issued coupons).
    /// </summary>
    public void TryDeactivateWaitlistMode(TicketTypeId ticketTypeId)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistMode) return;

        if (ticketType.MaxCapacity.HasValue
            && ticketType.UsedCapacity < ticketType.MaxCapacity.Value)
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

    private void EnsureEventActive()
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
    /// cancelled IDs, or overlapping time slots. Does not modify capacity.
    /// Use this before delta-based claim/release operations to enforce invariants
    /// on the full new selection.
    /// </summary>
    public void ValidateSelection(IReadOnlyList<TicketTypeId> ids)
    {
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
    /// If enforce is true, capacity is enforced and self-service flag is checked (self-service path).
    /// If enforce is false, UsedCapacity is incremented without enforcement (admin/coupon path).
    /// Returns snapshots of the claimed ticket types.
    /// </summary>
    public IReadOnlyList<TicketTypeSnapshot> Claim(IReadOnlyList<TicketTypeId> ids, bool enforce)
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

        if (enforce)
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
            if (enforce)
                ticketType.ClaimWithEnforcement();
            else
                ticketType.ClaimUncapped();

            // Activate WaitlistMode when the last slot is claimed on a WaitlistEnabled type
            if (enforce && ticketType.WaitlistEnabled && !ticketType.WaitlistMode
                && ticketType.MaxCapacity.HasValue
                && ticketType.UsedCapacity >= ticketType.MaxCapacity.Value)
            {
                ticketType.ActivateWaitlistMode();
                AddDomainEvent(new WaitlistModeActivatedDomainEvent(Id, id));
            }
        }

        return ids.Select(id =>
        {
            var ticketType = ticketTypeMap[id];
            return new TicketTypeSnapshot(id, ticketType.Name, ticketType.TimeSlots);
        }).ToList();
    }

    /// <summary>
    /// Releases capacity for the given ticket type IDs. Unknown IDs are silently skipped.
    /// UsedCapacity is clamped at zero.
    /// </summary>
    public void Release(IReadOnlyList<TicketTypeId> ids)
    {
        foreach (var id in ids)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == id);
            ticketType?.ReleaseCapacity();
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

        public static readonly Error EventNotActive = new(
            "ticket_catalog.event_not_active",
            "Operation not allowed: the ticketed event is not Active.",
            Type: ErrorType.Validation);
    }
}
