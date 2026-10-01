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
        int? publicCapacity,
        bool selfServiceEnabled = true,
        bool waitlistEnabled = false,
        int claimWindowHours = 8,
        ReconfirmationEmailLimit? maxReconfirmationEmails = null)
    {
        EnsureEventActive();

        if (_ticketTypes.Any(tt => string.Equals(tt.Name.Value, name.Value, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypeName(name));

        if (waitlistEnabled && publicCapacity is null)
            throw new BusinessRuleViolationException(Errors.WaitlistRequiresBoundedCapacity(id));

        _ticketTypes.Add(new TicketType(id, name, timeSlots, publicCapacity, selfServiceEnabled, waitlistEnabled, claimWindowHours, maxReconfirmationEmails));
        AddDomainEvent(new TicketCatalogSelfServiceTicketTypeCountChangedDomainEvent(
            TeamId,
            Id,
            Version,
            _ticketTypes.Count(t => t.SelfServiceEnabled)));

        // A newly added waitlist-enabled type with a public capacity of 0 is sold out straight away.
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
        int? publicCapacity,
        bool? selfServiceEnabled = null,
        bool? waitlistEnabled = null,
        int? claimWindowHours = null,
        ReconfirmationEmailLimit? maxReconfirmationEmails = null,
        bool updateMaxReconfirmationEmails = false)
    {
        EnsureEventActive();

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

        // Removing the capacity limit or explicitly disabling the waitlist switches the waitlist off.
        // Removing the limit takes precedence: with unbounded capacity everyone waiting can be offered a
        // coupon, whereas an explicit disable removes everyone still waiting.
        var removingCapacityLimit = publicCapacity is null && ticketType.WaitlistEnabled;
        var disablingWaitlist = !removingCapacityLimit && waitlistEnabled == false && ticketType.WaitlistEnabled;

        if (removingCapacityLimit || disablingWaitlist)
        {
            var wasInWaitlistMode = ticketType.WaitlistMode;

            ticketType.UpdateCapacity(publicCapacity);

            if (wasInWaitlistMode)
                ticketType.DeactivateWaitlistMode();
            ticketType.DisableWaitlist();

            if (removingCapacityLimit)
            {
                AddDomainEvent(new WaitlistCapacityLimitRemovedDomainEvent(TeamId, Id, id));
            }
            else
            {
                // Seats available after the same update go to the front of the queue before the rest is removed.
                var freedSlots = wasInWaitlistMode ? Math.Max(0, ticketType.AvailableCapacity ?? 0) : 0;
                AddDomainEvent(new WaitlistDisabledDomainEvent(TeamId, Id, id, freedSlots));
            }

            var waitlistOffSelfServiceCount = _ticketTypes.Count(t => t.SelfServiceEnabled);
            if (waitlistOffSelfServiceCount != previousSelfServiceCount)
            {
                AddDomainEvent(new TicketCatalogSelfServiceTicketTypeCountChangedDomainEvent(
                    TeamId,
                    Id,
                    Version,
                    waitlistOffSelfServiceCount));
            }
            return;
        }

        // Enabling waitlist
        if (waitlistEnabled == true && !ticketType.WaitlistEnabled)
        {
            var effectivePublicCapacity = publicCapacity ?? ticketType.PublicCapacity;
            if (effectivePublicCapacity is null)
                throw new BusinessRuleViolationException(Errors.WaitlistRequiresBoundedCapacity(id));

            ticketType.EnableWaitlist();
        }

        // Update capacity and handle freed seats or retroactive activation
        var previousPublicCapacity = ticketType.PublicCapacity;
        ticketType.UpdateCapacity(publicCapacity);

        // Retroactive WaitlistMode activation: enabled on a sold-out type
        if (ticketType.WaitlistEnabled && !ticketType.WaitlistMode && ticketType.IsSoldOut)
        {
            ticketType.ActivateWaitlistMode();
            AddDomainEvent(new WaitlistModeActivatedDomainEvent(TeamId, Id, id));
        }
        // Raising PublicCapacity while in WaitlistMode can make seats available to the people waiting, once any
        // outstanding waitlist holds (and any shortfall from an earlier lowering) are covered.
        else if (ticketType.PublicCapacity != previousPublicCapacity)
        {
            RaiseWaitlistCapacityAvailable(ticketType);
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
    /// Re-evaluates WaitlistMode for the given ticket type. Clears WaitlistMode only when all three conditions
    /// hold: nobody queued (<see cref="TicketType.WaitlistQueuedCount"/>), no outstanding waitlist offers
    /// (<see cref="TicketType.WaitlistHeldCapacity"/>), and seats available to the public. Both counts live on the
    /// catalog, so a concurrent waitlist join or offer on the same ticket type makes one of the two saves fail.
    /// </summary>
    public void ReEvaluateWaitlistMode(TicketTypeId ticketTypeId)
    {
        EnsureEventActive();

        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistMode) return;

        if (ticketType.WaitlistQueuedCount == 0
            && ticketType.WaitlistHeldCapacity == 0
            && !ticketType.IsSoldOut)
        {
            ticketType.DeactivateWaitlistMode();
        }
    }

    /// <summary>
    /// Counts an attendee joining the ticket type's waitlist queue, in the same unit of work as the
    /// <see cref="Waitlist"/> entry is added.
    /// </summary>
    public void JoinWaitlistQueue(TicketTypeId ticketTypeId)
    {
        EnsureEventActive();

        FindTicketType(ticketTypeId).JoinWaitlistQueue();
    }

    /// <summary>
    /// Counts an attendee leaving the ticket type's waitlist queue, in the same unit of work as the
    /// <see cref="Waitlist"/> entry is removed. Unknown IDs are silently skipped.
    /// </summary>
    public void LeaveWaitlistQueue(TicketTypeId ticketTypeId)
    {
        GetTicketType(ticketTypeId)?.LeaveWaitlistQueue();
    }

    /// <summary>
    /// Holds a public seat on the ticket type for an automatic waitlist offer, in the same unit of work as the
    /// offer's coupon is issued. VIP offers take no hold: they are admin tickets.
    /// </summary>
    public void HoldForWaitlistOffer(TicketTypeId ticketTypeId)
    {
        EnsureEventActive();

        FindTicketType(ticketTypeId).HoldForWaitlistOffer();
    }

    /// <summary>
    /// Gives back the seat held by an automatic waitlist offer that lapsed unclaimed. Unknown IDs are silently skipped.
    /// Raises <see cref="WaitlistCapacityAvailableDomainEvent"/> when that leaves a seat for the next person waiting.
    /// </summary>
    public void ReleaseWaitlistHold(TicketTypeId ticketTypeId)
    {
        var ticketType = GetTicketType(ticketTypeId);
        if (ticketType is null) return;

        ticketType.ReleaseWaitlistHold();
        RaiseWaitlistCapacityAvailable(ticketType);
    }

    /// <summary>
    /// Clears WaitlistMode for a ticket type whatever its availability, once the catalog counts nobody queued
    /// (<see cref="TicketType.WaitlistQueuedCount"/>) and no outstanding waitlist offers
    /// (<see cref="TicketType.WaitlistHeldCapacity"/>). Called when the <see cref="Waitlist"/> is exhausted; reading the
    /// catalog's own counts means a concurrent waitlist join on the same ticket type makes one of the two saves fail.
    /// </summary>
    public void LiftWaitlistModeWhenExhausted(TicketTypeId ticketTypeId)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistMode) return;

        if (ticketType.WaitlistQueuedCount == 0 && ticketType.WaitlistHeldCapacity == 0)
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
    /// Returns snapshots of the given ticket types without claiming any capacity, e.g. to describe
    /// the ticket types an attendee is waitlisted for. IDs not in the catalog are skipped.
    /// </summary>
    public IReadOnlyList<TicketTypeSnapshot> DescribeTicketTypes(IEnumerable<TicketTypeId> ids) =>
        ids.Select(GetTicketType)
            .OfType<TicketType>()
            .Select(ticketType => new TicketTypeSnapshot(ticketType.Id, ticketType.Name, ticketType.TimeSlots))
            .ToList();

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
    /// <see cref="ClaimMode.Public"/> is a self-service claim: it enforces public capacity and requires self-service
    /// to be enabled. <see cref="ClaimMode.Admin"/> is never enforced and comes on top of public capacity.
    /// Returns snapshots of the claimed ticket types, tagged with the mode used so a later
    /// <see cref="Release"/> credits the correct pool back.
    /// </summary>
    public IReadOnlyList<TicketTypeSnapshot> Claim(IReadOnlyList<TicketTypeId> ids, ClaimMode mode)
        => Claim(ids, mode, redeemsWaitlistOffer: false);

    /// <summary>
    /// Claims the ticket types a coupon redemption granted, in the coupon's pool
    /// (<see cref="Coupon.RedemptionClaimMode"/>). Never enforced: an automatic waitlist offer turns its hold into a
    /// public ticket; any other coupon (organiser or VIP) claims admin tickets.
    /// </summary>
    public IReadOnlyList<TicketTypeSnapshot> ClaimWithCoupon(IReadOnlyList<TicketTypeId> ids, Coupon coupon)
        => coupon.RedemptionClaimMode == ClaimMode.Public
            ? Claim(ids, ClaimMode.Public, redeemsWaitlistOffer: true)
            : Claim(ids, ClaimMode.Admin, redeemsWaitlistOffer: false);

    private IReadOnlyList<TicketTypeSnapshot> Claim(
        IReadOnlyList<TicketTypeId> ids,
        ClaimMode mode,
        bool redeemsWaitlistOffer)
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

        var selfService = mode == ClaimMode.Public && !redeemsWaitlistOffer;
        if (selfService)
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
            if (redeemsWaitlistOffer)
                ticketType.ClaimWaitlistOffer();
            else
                ticketType.Claim(mode);

            // Activate WaitlistMode when the last public seat is claimed on a WaitlistEnabled type
            if (selfService && ticketType.WaitlistEnabled && !ticketType.WaitlistMode && ticketType.IsSoldOut)
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
    /// Each snapshot's <see cref="TicketTypeSnapshot.Mode"/> determines which pool is
    /// credited back (see <see cref="TicketType.ReleaseCapacity"/>); counters are clamped at zero.
    /// Raises <see cref="WaitlistCapacityAvailableDomainEvent"/> for each ticket type whose freed public seats leave
    /// a seat for the people waiting. Releasing an admin ticket frees no public seat, so it raises nothing.
    /// </summary>
    public void Release(IReadOnlyList<TicketTypeSnapshot> tickets)
    {
        var publicSeatsFreed = new List<TicketType>();
        foreach (var ticket in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == ticket.Id);
            if (ticketType is null) continue;

            ticketType.ReleaseCapacity(ticket.Mode);
            if (ticket.Mode == ClaimMode.Public)
                publicSeatsFreed.Add(ticketType);
        }

        foreach (var ticketType in publicSeatsFreed.Distinct())
            RaiseWaitlistCapacityAvailable(ticketType);
    }

    /// <summary>
    /// Raises <see cref="WaitlistCapacityAvailableDomainEvent"/> when the ticket type is in WaitlistMode and has
    /// public seats that are neither used nor held by an outstanding automatic waitlist offer.
    /// </summary>
    private void RaiseWaitlistCapacityAvailable(TicketType ticketType)
    {
        if (ticketType.WaitlistMode && ticketType.AvailableCapacity is int available and > 0)
            AddDomainEvent(new WaitlistCapacityAvailableDomainEvent(TeamId, Id, ticketType.Id, available));
    }

    public TicketType FindTicketType(TicketTypeId id)
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
                "WaitlistEnabled requires a bounded capacity (PublicCapacity must be set).",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static readonly Error EventNotActive = new(
            "ticket_catalog.event_not_active",
            "Operation not allowed: the ticketed event is not Active.",
            Type: ErrorType.Validation);
    }
}
