using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Entities;

/// <summary>
/// Records a request to materialise a new <c>TicketedEvent</c> in the Registrations module.
/// Owned by <see cref="Team"/>. Bounded by <see cref="Team.PendingEventCount"/> while in
/// <see cref="TeamEventCreationRequestStatus.Pending"/>; older terminal records are pruned by
/// a separate maintenance job.
/// </summary>
public class TeamEventCreationRequest : Entity<CreationRequestId>
{
    // Required for EF Core
    private TeamEventCreationRequest()
    {
    }

    private TeamEventCreationRequest(
        CreationRequestId id,
        Slug requestedSlug,
        UserId requesterId,
        DateTimeOffset requestedAt)
        : base(id)
    {
        RequestedSlug = requestedSlug;
        RequesterId = requesterId;
        RequestedAt = requestedAt;
        Status = TeamEventCreationRequestStatus.Pending;
    }

    public Slug RequestedSlug { get; private set; }
    public UserId RequesterId { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public TeamEventCreationRequestStatus Status { get; private set; }
    public TicketedEventId? TicketedEventId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Most recent <see cref="EventStatus"/> observed for the materialised
    /// <c>TicketedEvent</c>. Only meaningful once <see cref="Status"/> is
    /// <see cref="TeamEventCreationRequestStatus.Created"/>; used by the integration-event
    /// handlers to make cancel/archive counter swaps idempotent on redelivery.
    /// </summary>
    public EventStatus? ObservedEventStatus { get; private set; }

    public bool IsTerminal => Status != TeamEventCreationRequestStatus.Pending;

    internal static TeamEventCreationRequest Create(
        Slug requestedSlug,
        UserId requesterId,
        DateTimeOffset requestedAt) =>
        new(CreationRequestId.New(), requestedSlug, requesterId, requestedAt);

    internal void MarkCreated(TicketedEventId ticketedEventId, DateTimeOffset at)
    {
        EnsurePending();
        Status = TeamEventCreationRequestStatus.Created;
        TicketedEventId = ticketedEventId;
        CompletedAt = at;
        ObservedEventStatus = EventStatus.Active;
    }

    internal void RecordEventStatus(EventStatus newStatus)
    {
        ObservedEventStatus = newStatus;
    }

    internal void MarkRejected(string reason, DateTimeOffset at)
    {
        EnsurePending();
        Status = TeamEventCreationRequestStatus.Rejected;
        RejectionReason = reason;
        CompletedAt = at;
    }

    internal void MarkExpired(DateTimeOffset at)
    {
        EnsurePending();
        Status = TeamEventCreationRequestStatus.Expired;
        CompletedAt = at;
    }

    private void EnsurePending()
    {
        if (Status != TeamEventCreationRequestStatus.Pending)
        {
            throw new BusinessRuleViolationException(Errors.NotPending(Id, Status));
        }
    }

    internal static class Errors
    {
        public static Error NotPending(CreationRequestId id, TeamEventCreationRequestStatus status) =>
            new(
                "team_event_creation_request.not_pending",
                "The event creation request is not in a pending state.",
                Details: new Dictionary<string, object?>
                {
                    ["creationRequestId"] = id.Value,
                    ["status"] = status.ToString()
                });
    }
}
