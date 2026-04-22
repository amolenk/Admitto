using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
/// <remarks>
/// The team owns four bounded counters that track the team-side view of the events it has
/// requested in the Registrations module:
/// <list type="bullet">
///   <item><see cref="ActiveEventCount"/> — materialised events currently Active.</item>
///   <item><see cref="CancelledEventCount"/> — materialised events currently Cancelled.</item>
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
        Slug slug,
        DisplayName name,
        EmailAddress emailAddress,
        DateTimeOffset? archivedAt)
        : base(id)
    {
        Slug = slug;
        Name = name;
        EmailAddress = emailAddress;
        ArchivedAt = archivedAt;
    }

    public Slug Slug { get; private set; }
    public DisplayName Name { get; private set; }
    public EmailAddress EmailAddress { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public int ActiveEventCount { get; private set; }
    public int CancelledEventCount { get; private set; }
    public int ArchivedEventCount { get; private set; }
    public int PendingEventCount { get; private set; }

    public IReadOnlyList<TeamEventCreationRequest> EventCreationRequests =>
        _eventCreationRequests.AsReadOnly();

    public bool IsArchived => ArchivedAt.HasValue;

    public static Team Create(
        Slug slug,
        DisplayName name,
        EmailAddress emailAddress) =>
        new(
            TeamId.New(),
            slug,
            name,
            emailAddress,
            archivedAt: null);

    public void ChangeName(DisplayName name)
    {
        EnsureNotArchived();
        Name = name;
    }

    public void ChangeEmailAddress(EmailAddress emailAddress)
    {
        EnsureNotArchived();
        EmailAddress = emailAddress;
    }

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
    }

    /// <summary>
    /// Records a request to materialise a new ticketed event under this team. Increments
    /// <see cref="PendingEventCount"/> and adds a <see cref="TeamEventCreationRequest"/> in
    /// <see cref="TeamEventCreationRequestStatus.Pending"/>. Returns the surrogate
    /// <see cref="CreationRequestId"/> used to correlate the eventual response from
    /// Registrations.
    /// </summary>
    public TeamEventCreationRequest RequestEventCreation(
        Slug requestedSlug,
        UserId requesterId,
        DateTimeOffset requestedAt)
    {
        EnsureNotArchived();

        var request = TeamEventCreationRequest.Create(requestedSlug, requesterId, requestedAt);
        _eventCreationRequests.Add(request);
        PendingEventCount++;

        return request;
    }

    /// <summary>
    /// Records a request to materialise a new ticketed event under this team and raises the
    /// <see cref="TicketedEventCreationRequestedDomainEvent"/> that outboxes the corresponding
    /// integration event for Registrations. Same invariants as
    /// <see cref="RequestEventCreation(Slug,UserId,DateTimeOffset)"/>.
    /// </summary>
    public TeamEventCreationRequest RequestEventCreation(
        Slug requestedSlug,
        DisplayName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        UserId requesterId,
        DateTimeOffset requestedAt)
    {
        var request = RequestEventCreation(requestedSlug, requesterId, requestedAt);

        AddDomainEvent(new TicketedEventCreationRequestedDomainEvent(
            request.Id,
            Id,
            requestedSlug,
            name,
            websiteUrl,
            baseUrl,
            startsAt,
            endsAt));

        return request;
    }

    /// <summary>
    /// Marks an in-flight creation request as <see cref="TeamEventCreationRequestStatus.Created"/>
    /// in response to a <c>TicketedEventCreated</c> integration event. Idempotent: if the request
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
    /// in response to a <c>TicketedEventCreationRejected</c> integration event. Idempotent.
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
    /// <see cref="EventStatus.Cancelled"/>. Idempotent on the observed status.
    /// </summary>
    public void RegisterEventCancelled(TicketedEventId ticketedEventId)
    {
        var request = FindRequestForEvent(ticketedEventId);
        if (request is null)
        {
            return;
        }

        if (request.ObservedEventStatus != EventStatus.Active)
        {
            return;
        }

        request.RecordEventStatus(EventStatus.Cancelled);
        if (ActiveEventCount > 0) ActiveEventCount--;
        CancelledEventCount++;
    }

    /// <summary>
    /// Records that the materialised event with the given id transitioned to
    /// <see cref="EventStatus.Archived"/>. The source counter (active or cancelled) is
    /// determined from the observed status. Idempotent.
    /// </summary>
    public void RegisterEventArchived(TicketedEventId ticketedEventId)
    {
        var request = FindRequestForEvent(ticketedEventId);
        if (request is null)
        {
            return;
        }

        switch (request.ObservedEventStatus)
        {
            case EventStatus.Active:
                if (ActiveEventCount > 0) ActiveEventCount--;
                break;
            case EventStatus.Cancelled:
                if (CancelledEventCount > 0) CancelledEventCount--;
                break;
            case EventStatus.Archived:
                return;
            default:
                return;
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
