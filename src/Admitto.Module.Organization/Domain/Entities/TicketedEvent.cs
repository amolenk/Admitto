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
        TimeWindow eventWindow)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        WebsiteUrl = websiteUrl;
        BaseUrl = baseUrl;
        EventWindow = eventWindow;
        Status = EventStatus.Active;

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
            eventWindow);

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

        AddDomainEvent(new TicketedEventCancelledDomainEvent(Id));
    }

    public void Archive()
    {
        if (Status == EventStatus.Archived)
        {
            throw new BusinessRuleViolationException(Errors.EventAlreadyArchived(Id));
        }

        Status = EventStatus.Archived;

        AddDomainEvent(new TicketedEventArchivedDomainEvent(Id));
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
    }
}