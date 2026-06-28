using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Organization.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
/// <remarks>
/// The team owns three bounded counters that track the team-side view of the events it has
/// requested in the Registrations module:
/// <list type="bullet">
///   <item><see cref="ActiveEventCount"/> — materialised events currently Active.</item>
///   <item><see cref="ArchivedEventCount"/> — materialised events that have reached Archived.</item>
///   <item><see cref="PendingEventCount"/> — in-flight creation requests not yet acked by Registrations.</item>
/// </list>
/// Counters are advanced/rolled back by the integration-event handlers in
/// <c>TeamManagement/EventCreationLifecycle</c>; archive is gated by
/// <c>ActiveEventCount == 0 &amp;&amp; PendingEventCount == 0</c>.
/// </remarks>
public class Team : Aggregate<TeamId>
{
    private readonly List<TeamEventCreationRequest> _eventCreationRequests = [];

    // ReSharper disable once UnusedMember.Local
    // Required for EF Core
    private Team()
    {
    }

    private Team(
        TeamId id,
        TeamName name,
        TeamAccentColor accentColor,
        DateTimeOffset? archivedAt)
        : base(id)
    {
        Name = name;
        AccentColor = accentColor;
        ArchivedAt = archivedAt;
    }

    public TeamName Name { get; private set; }
    public TeamAccentColor AccentColor { get; private set; } = TeamAccentColor.From(TeamAccentColor.Default);
    public DateTimeOffset? ArchivedAt { get; private set; }

    public int ActiveEventCount { get; private set; }
    public int ArchivedEventCount { get; private set; }
    public int PendingEventCount { get; private set; }

    public IReadOnlyList<TeamEventCreationRequest> EventCreationRequests =>
        _eventCreationRequests.AsReadOnly();

    public bool IsArchived => ArchivedAt.HasValue;

    public static Team Create(TeamName name, TeamAccentColor? accentColor = null)
    {
        var team = new Team(
            TeamId.New(),
            name,
            accentColor ?? TeamAccentColor.From(TeamAccentColor.Default),
            archivedAt: null);

        team.AddDomainEvent(new TeamCreatedDomainEvent(
            team.Id,
            team.Name,
            team.AccentColor,
            team.Version));

        return team;
    }

    public void UpdateDetails(TeamName? name, TeamAccentColor? accentColor)
    {
        EnsureNotArchived();

        var newName = name ?? Name;
        var newAccentColor = accentColor ?? AccentColor;

        if (Name == newName && AccentColor == newAccentColor)
            return;

        Name = newName;
        AccentColor = newAccentColor;

        AddDomainEvent(new TeamDetailsUpdatedDomainEvent(
            Id,
            Name,
            AccentColor,
            Version));
    }

    public void ChangeName(TeamName name) => UpdateDetails(name, accentColor: null);

    public void ChangeAccentColor(TeamAccentColor accentColor)
        => UpdateDetails(name: null, accentColor);

    public void Archive(DateTimeOffset archivedAt)
    {
        if (IsArchived)
        {
            throw new BusinessRuleViolationException(Errors.TeamAlreadyArchived(Id));
        }

        if (ActiveEventCount > 0 || PendingEventCount > 0)
        {
            throw new BusinessRuleViolationException(
                Errors.HasActiveOrPendingEvents(Id, ActiveEventCount, PendingEventCount));
        }

        ArchivedAt = archivedAt;
        AddDomainEvent(new TeamArchivedDomainEvent(Id));
    }

    /// <summary>
    /// Records a request to materialise a new ticketed event under this team. Increments
    /// <see cref="PendingEventCount"/> and adds a <see cref="TeamEventCreationRequest"/> in
    /// <see cref="TeamEventCreationRequestStatus.Pending"/>. Returns the surrogate
    /// <see cref="CreationRequestId"/> used to correlate the eventual response from
    /// Registrations.
    /// </summary>
    public TeamEventCreationRequest RequestEventCreation(
        UserId requesterId,
        DateTimeOffset requestedAt)
    {
        EnsureNotArchived();

        var request = TeamEventCreationRequest.Create(requesterId, requestedAt);
        _eventCreationRequests.Add(request);
        PendingEventCount++;

        return request;
    }

    /// <summary>
    /// Records a request to materialise a new ticketed event under this team and raises the
    /// <see cref="TicketedEventCreationRequestedDomainEvent"/> that outboxes the corresponding
    /// integration event for Registrations. Same invariants as
    /// <see cref="RequestEventCreation(UserId,DateTimeOffset)"/>.
    /// </summary>
    public TeamEventCreationRequest RequestEventCreation(
        EventName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        Slug publicSlug,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        TimeZoneId timeZone,
        UserId requesterId,
        DateTimeOffset requestedAt)
    {
        var request = RequestEventCreation(requesterId, requestedAt);

        AddDomainEvent(new TicketedEventCreationRequestedDomainEvent(
            request.Id,
            Id,
            name,
            websiteUrl,
            baseUrl,
            startsAt,
            endsAt,
            timeZone,
            publicSlug));

        return request;
    }

    /// <summary>
    /// Marks an in-flight creation request as <see cref="TeamEventCreationRequestStatus.Created"/>
    /// in response to a <c>TicketedEventCreatedIntegrationEvent</c> integration event. Idempotent: if the request
    /// is already terminal, returns without mutating any counters.
    /// </summary>
    public void RegisterEventCreated(
        CreationRequestId creationRequestId,
        TicketedEventId ticketedEventId,
        DateTimeOffset at)
    {
        var request = _eventCreationRequests.FirstOrDefault(r => r.Id == creationRequestId);
        if (request is null || request.IsTerminal)
        {
            return;
        }

        request.MarkCreated(ticketedEventId, at);
        PendingEventCount--;
        ActiveEventCount++;
    }

    /// <summary>
    /// Marks an in-flight creation request as <see cref="TeamEventCreationRequestStatus.Rejected"/>
    /// in response to a <c>TicketedEventCreationRejectedIntegrationEvent</c> integration event. Idempotent.
    /// </summary>
    public void RegisterEventCreationRejected(
        CreationRequestId creationRequestId,
        string reason,
        DateTimeOffset at)
    {
        var request = _eventCreationRequests.FirstOrDefault(r => r.Id == creationRequestId);
        if (request is null || request.IsTerminal)
        {
            return;
        }

        request.MarkRejected(reason, at);
        PendingEventCount--;
    }

    /// <summary>
    /// Marks an in-flight creation request as <see cref="TeamEventCreationRequestStatus.Expired"/>
    /// from the maintenance job. Idempotent.
    /// </summary>
    public void ExpireEventCreationRequest(CreationRequestId creationRequestId, DateTimeOffset at)
    {
        var request = _eventCreationRequests.FirstOrDefault(r => r.Id == creationRequestId);
        if (request is null || request.IsTerminal)
        {
            return;
        }

        request.MarkExpired(at);
        PendingEventCount--;
    }

    /// <summary>
    /// Records that the materialised event with the given id transitioned to
    /// <see cref="EventStatus.Archived"/>. Idempotent.
    /// </summary>
    public void RegisterEventArchived(TicketedEventId ticketedEventId)
    {
        var request = FindRequestForEvent(ticketedEventId);
        if (request is null)
        {
            return;
        }

        if (request.ObservedEventStatus == EventStatus.Archived)
        {
            return;
        }

        if (request.ObservedEventStatus == EventStatus.Active)
        {
            if (ActiveEventCount > 0) ActiveEventCount--;
        }

        request.RecordEventStatus(EventStatus.Archived);
        ArchivedEventCount++;
    }

    public void EnsureNotArchived()
    {
        if (IsArchived)
        {
            throw new BusinessRuleViolationException(Errors.TeamArchived(Id));
        }
    }

    private TeamEventCreationRequest? FindRequestForEvent(TicketedEventId ticketedEventId) =>
        _eventCreationRequests.FirstOrDefault(r =>
            r.TicketedEventId.HasValue && r.TicketedEventId.Value == ticketedEventId);

    internal static class Errors
    {
        public static Error TeamArchived(TeamId teamId) =>
            new(
                "team.archived",
                "The team is archived.",
                Details: new Dictionary<string, object?>
                {
                    ["teamId"] = teamId.Value
                });

        public static Error TeamAlreadyArchived(TeamId teamId) =>
            new(
                "team.already_archived",
                "The team is already archived.",
                Details: new Dictionary<string, object?>
                {
                    ["teamId"] = teamId.Value
                });

        public static Error HasActiveOrPendingEvents(TeamId teamId, int active, int pending) =>
            new(
                "team.has_active_or_pending_events",
                "The team has active or pending ticketed events.",
                Type: ErrorType.Validation,
                Details: new Dictionary<string, object?>
                {
                    ["teamId"] = teamId.Value,
                    ["activeEventCount"] = active,
                    ["pendingEventCount"] = pending
                });
    }
}
