using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
/// <remarks>
/// On creation a <see cref="TicketedEventCreatedDomainEvent"/> is raised so that the owning
/// <see cref="Team"/> can update its <see cref="Team.TicketedEventScopeVersion"/>, which
/// ensures the team row is modified in the same transaction. This closes the TOCTOU window
/// between the active-events guard in <c>ArchiveTeamHandler</c> and the archive commit.
/// </remarks>
public class TicketedEvent : Aggregate<TicketedEventId>
{
    private readonly List<TicketType> _ticketTypes = [];

    // ReSharper disable once UnusedMember.Local
    // Required for EF Core
    private TicketedEvent()
    {
    }

    private TicketedEvent(
        TicketedEventId id,
        TeamId teamId,
        Slug slug,
        DisplayName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        TimeWindow eventWindow,
        IReadOnlyList<TicketType> ticketTypes)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        WebsiteUrl = websiteUrl;
        BaseUrl = baseUrl;
        EventWindow = eventWindow;
        Status = EventStatus.Active;

        _ticketTypes = ticketTypes.ToList();

        // Notify the owning team so it can update its TicketedEventScopeVersion.
        AddDomainEvent(new TicketedEventCreatedDomainEvent(teamId));
    }

    public TeamId TeamId { get; private set; }
    public Slug Slug { get; private set; }
    public DisplayName Name { get; private set; }
    public AbsoluteUrl WebsiteUrl { get; private set; }
    public AbsoluteUrl BaseUrl { get; private set; }
    public TimeWindow EventWindow { get; private set; } = null!;
    public EventStatus Status { get; private set; }
    public IReadOnlyList<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public static TicketedEvent Create(
        TeamId teamId,
        Slug slug,
        DisplayName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        TimeWindow eventWindow) =>
        new(
            TicketedEventId.New(),
            teamId,
            slug,
            name,
            websiteUrl,
            baseUrl,
            eventWindow,
            []);

    public void Update(
        DisplayName? name,
        AbsoluteUrl? websiteUrl,
        AbsoluteUrl? baseUrl,
        TimeWindow? eventWindow)
    {
        EnsureNotCancelledOrArchived();

        if (name is not null) Name = name.Value;
        if (websiteUrl is not null) WebsiteUrl = websiteUrl.Value;
        if (baseUrl is not null) BaseUrl = baseUrl.Value;
        if (eventWindow is not null) EventWindow = eventWindow;
    }

    public void Cancel()
    {
        if (Status == EventStatus.Archived)
        {
            throw new BusinessRuleViolationException(Errors.EventArchived(Id));
        }

        if (Status == EventStatus.Cancelled)
        {
            throw new BusinessRuleViolationException(Errors.EventAlreadyCancelled(Id));
        }

        Status = EventStatus.Cancelled;

        for (var i = 0; i < _ticketTypes.Count; i++)
        {
            if (!_ticketTypes[i].IsCancelled)
            {
                _ticketTypes[i] = _ticketTypes[i] with { IsCancelled = true };
            }
        }
    }

    public void Archive()
    {
        if (Status == EventStatus.Archived)
        {
            throw new BusinessRuleViolationException(Errors.EventAlreadyArchived(Id));
        }

        Status = EventStatus.Archived;
    }

    public void AddTicketType(
        Slug slug,
        DisplayName name,
        TimeSlot[] timeSlots,
        Capacity? capacity)
    {
        EnsureNotCancelledOrArchived();

        if (_ticketTypes.Any(tt => tt.Slug == slug))
        {
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypeSlug(slug));
        }

        _ticketTypes.Add(new TicketType(slug, name, timeSlots, capacity));

        AddDomainEvent(new TicketTypeAddedDomainEvent(
            Id,
            slug.Value,
            name.Value,
            timeSlots.Select(ts => ts.Slug.Value).ToArray(),
            capacity?.Value));
    }

    public void UpdateTicketType(
        Slug slug,
        DisplayName? name,
        Capacity? capacity)
    {
        EnsureNotCancelledOrArchived();

        var index = FindTicketTypeIndex(slug);
        var existing = _ticketTypes[index];

        if (existing.IsCancelled)
        {
            throw new BusinessRuleViolationException(Errors.TicketTypeAlreadyCancelled(slug));
        }

        if (capacity != existing.Capacity)
        {
            AddDomainEvent(new TicketTypeCapacityChangedDomainEvent(Id, slug.Value, capacity?.Value));
        }

        _ticketTypes[index] = existing with
        {
            Name = name ?? existing.Name,
            Capacity = capacity
        };
    }

    public void CancelTicketType(Slug slug)
    {
        var index = FindTicketTypeIndex(slug);
        var existing = _ticketTypes[index];

        if (existing.IsCancelled)
        {
            throw new BusinessRuleViolationException(Errors.TicketTypeAlreadyCancelled(slug));
        }

        _ticketTypes[index] = existing with { IsCancelled = true };
    }

    private void EnsureNotCancelledOrArchived()
    {
        if (Status == EventStatus.Cancelled)
        {
            throw new BusinessRuleViolationException(Errors.EventCancelled(Id));
        }

        if (Status == EventStatus.Archived)
        {
            throw new BusinessRuleViolationException(Errors.EventArchived(Id));
        }
    }

    private int FindTicketTypeIndex(Slug slug)
    {
        var index = _ticketTypes.FindIndex(tt => tt.Slug == slug);
        if (index < 0)
        {
            throw new BusinessRuleViolationException(Errors.TicketTypeNotFound(slug));
        }

        return index;
    }

    internal static class Errors
    {
        public static Error EventCancelled(TicketedEventId eventId) =>
            new(
                "event.cancelled",
                "The event is cancelled.",
                Details: new Dictionary<string, object?> { ["eventId"] = eventId.Value });

        public static Error EventAlreadyCancelled(TicketedEventId eventId) =>
            new(
                "event.already_cancelled",
                "The event is already cancelled.",
                Details: new Dictionary<string, object?> { ["eventId"] = eventId.Value });

        public static Error EventArchived(TicketedEventId eventId) =>
            new(
                "event.archived",
                "The event is archived.",
                Details: new Dictionary<string, object?> { ["eventId"] = eventId.Value });

        public static Error EventAlreadyArchived(TicketedEventId eventId) =>
            new(
                "event.already_archived",
                "The event is already archived.",
                Details: new Dictionary<string, object?> { ["eventId"] = eventId.Value });

        public static Error DuplicateTicketTypeSlug(Slug slug) =>
            new(
                "event.duplicate_ticket_type_slug",
                "A ticket type with this slug already exists.",
                Details: new Dictionary<string, object?> { ["slug"] = slug.Value });

        public static Error TicketTypeNotFound(Slug slug) =>
            new(
                "event.ticket_type_not_found",
                "Ticket type could not be found.",
                Type: ErrorType.NotFound,
                Details: new Dictionary<string, object?> { ["slug"] = slug.Value });

        public static Error TicketTypeAlreadyCancelled(Slug slug) =>
            new(
                "event.ticket_type_already_cancelled",
                "The ticket type is already cancelled.",
                Details: new Dictionary<string, object?> { ["slug"] = slug.Value });
    }
}