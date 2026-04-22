using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

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
    /// atomic capacity claim can refuse to run once the event has been cancelled or
    /// archived, even if a registration handler's earlier policy check observed Active.
    /// Transitions are one-way: Active → Cancelled, Active → Archived, Cancelled → Archived.
    /// </summary>
    public EventLifecycleStatus EventStatus { get; private set; } = EventLifecycleStatus.Active;

    public static TicketCatalog Create(TicketedEventId eventId) => new(eventId);

    /// <summary>
    /// Transitions <see cref="EventStatus"/> to <see cref="EventLifecycleStatus.Cancelled"/>.
    /// Idempotent when already Cancelled; rejected when the catalog is already Archived.
    /// </summary>
    public void MarkEventCancelled()
    {
        if (EventStatus == EventLifecycleStatus.Cancelled) return;

        if (EventStatus == EventLifecycleStatus.Archived)
            throw new BusinessRuleViolationException(Errors.IllegalEventStatusTransition);

        EventStatus = EventLifecycleStatus.Cancelled;
    }

    /// <summary>
    /// Transitions <see cref="EventStatus"/> to <see cref="EventLifecycleStatus.Archived"/>.
    /// Idempotent when already Archived. Legal from Active or Cancelled.
    /// </summary>
    public void MarkEventArchived()
    {
        if (EventStatus == EventLifecycleStatus.Archived) return;

        EventStatus = EventLifecycleStatus.Archived;
    }

    public void AddTicketType(
        Slug slug,
        DisplayName name,
        TimeSlot[] timeSlots,
        int? maxCapacity)
    {
        EnsureEventActive();

        if (_ticketTypes.Any(tt => tt.Id == slug.Value))
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypeSlug(slug));

        _ticketTypes.Add(new TicketType(slug.Value, name, timeSlots, maxCapacity));
    }

    public void UpdateTicketType(
        Slug slug,
        DisplayName? name,
        int? maxCapacity)
    {
        EnsureEventActive();

        var ticketType = FindTicketType(slug);

        if (ticketType.IsCancelled)
            throw new BusinessRuleViolationException(Errors.TicketTypeAlreadyCancelled(slug));

        if (name is not null)
            ticketType.UpdateName(name.Value);

        ticketType.UpdateCapacity(maxCapacity);
    }

    public void CancelTicketType(Slug slug)
    {
        EnsureEventActive();

        var ticketType = FindTicketType(slug);
        ticketType.Cancel();
    }

    private void EnsureEventActive()
    {
        if (EventStatus != EventLifecycleStatus.Active)
            throw new BusinessRuleViolationException(Errors.EventNotActive);
    }

    public TicketType? GetTicketType(string slug)
    {
        return _ticketTypes.FirstOrDefault(tt => tt.Id == slug);
    }

    /// <summary>
    /// Claims tickets for the given slugs. If enforce is true, capacity is enforced (self-service path).
    /// If enforce is false, UsedCapacity is incremented without enforcement (coupon path).
    /// </summary>
    public void Claim(IReadOnlyList<string> slugs, bool enforce)
    {
        EnsureEventActive();

        foreach (var slug in slugs)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == slug);
            if (ticketType is null)
                throw new BusinessRuleViolationException(Errors.TicketTypeNotFound(slug));

            if (enforce)
                ticketType.ClaimWithEnforcement();
            else
                ticketType.ClaimUncapped();
        }
    }

    private TicketType FindTicketType(Slug slug)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == slug.Value);
        if (ticketType is null)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotFound(slug.Value));

        return ticketType;
    }

    internal static class Errors
    {
        public static Error DuplicateTicketTypeSlug(Slug slug) =>
            new("ticket_catalog.duplicate_slug",
                "A ticket type with this slug already exists.",
                Details: new Dictionary<string, object?> { ["slug"] = slug.Value });

        public static Error TicketTypeNotFound(string slug) =>
            new("ticket_catalog.ticket_type_not_found",
                "Ticket type could not be found.",
                Type: ErrorType.NotFound,
                Details: new Dictionary<string, object?> { ["slug"] = slug });

        public static Error TicketTypeAlreadyCancelled(Slug slug) =>
            new("ticket_catalog.ticket_type_already_cancelled",
                "The ticket type is already cancelled.",
                Details: new Dictionary<string, object?> { ["slug"] = slug.Value });

        public static readonly Error EventNotActive = new(
            "ticket_catalog.event_not_active",
            "Operation not allowed: the ticketed event is not Active.",
            Type: ErrorType.Validation);

        public static readonly Error IllegalEventStatusTransition = new(
            "ticket_catalog.illegal_event_status_transition",
            "Illegal ticket catalog event status transition.");
    }
}
