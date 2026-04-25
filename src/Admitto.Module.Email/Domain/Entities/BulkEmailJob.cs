using Amolenk.Admitto.Module.Email.Domain.DomainEvents;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.Entities;

public sealed class BulkEmailJob : Aggregate<BulkEmailJobId>
{
    private readonly List<BulkEmailRecipient> _recipients = [];

    // Required for EF Core
    private BulkEmailJob()
    {
        EmailType = default!;
        Source = default!;
    }

    private BulkEmailJob(
        BulkEmailJobId id,
        TeamId teamId,
        TicketedEventId ticketedEventId,
        string emailType,
        string? subject,
        string? textBody,
        string? htmlBody,
        BulkEmailJobSource source,
        EmailAddress? triggeredBy,
        bool isSystemTriggered,
        DateTimeOffset createdAt)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        EmailType = emailType;
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
        Source = source;
        TriggeredBy = triggeredBy;
        IsSystemTriggered = isSystemTriggered;
        Status = BulkEmailJobStatus.Pending;
        CreatedAt = createdAt;
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId TicketedEventId { get; private set; }
    public string EmailType { get; private set; }
    public string? Subject { get; private set; }
    public string? TextBody { get; private set; }
    public string? HtmlBody { get; private set; }
    public BulkEmailJobSource Source { get; private set; }

    /// <summary>
    /// Email address of the user that triggered the job, or <c>null</c> for
    /// system-triggered jobs (e.g. scheduled reconfirm sends).
    /// </summary>
    public EmailAddress? TriggeredBy { get; private set; }

    /// <summary>
    /// <c>true</c> when the job was created by a scheduler (e.g. reconfirm
    /// trigger) rather than a real user.
    /// </summary>
    public bool IsSystemTriggered { get; private set; }

    public BulkEmailJobStatus Status { get; private set; }

    public IReadOnlyList<BulkEmailRecipient> Recipients => _recipients.AsReadOnly();

    public int RecipientCount { get; private set; }
    public int SentCount { get; private set; }
    public int FailedCount { get; private set; }
    public int CancelledCount { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancellationRequestedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public static BulkEmailJob Create(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        string emailType,
        string? subject,
        string? textBody,
        string? htmlBody,
        BulkEmailJobSource source,
        EmailAddress triggeredBy,
        DateTimeOffset now)
    {
        var job = new BulkEmailJob(
            BulkEmailJobId.New(),
            teamId,
            ticketedEventId,
            emailType,
            subject,
            textBody,
            htmlBody,
            source,
            triggeredBy,
            isSystemTriggered: false,
            createdAt: now);

        job.AddDomainEvent(new BulkEmailJobRequestedDomainEvent(job.Id, job.TeamId, job.TicketedEventId));
        return job;
    }

    public static BulkEmailJob CreateSystemTriggered(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        string emailType,
        string? subject,
        string? textBody,
        string? htmlBody,
        BulkEmailJobSource source,
        DateTimeOffset now)
    {
        var job = new BulkEmailJob(
            BulkEmailJobId.New(),
            teamId,
            ticketedEventId,
            emailType,
            subject,
            textBody,
            htmlBody,
            source,
            triggeredBy: null,
            isSystemTriggered: true,
            createdAt: now);

        job.AddDomainEvent(new BulkEmailJobRequestedDomainEvent(job.Id, job.TeamId, job.TicketedEventId));
        return job;
    }

    public void BeginResolving(DateTimeOffset now)
    {
        if (Status != BulkEmailJobStatus.Pending)
            throw new BusinessRuleViolationException(Errors.InvalidTransition(Status, BulkEmailJobStatus.Resolving));

        Status = BulkEmailJobStatus.Resolving;
        StartedAt ??= now;
    }

    public void BeginSending(IReadOnlyList<BulkEmailRecipient> recipients)
    {
        if (Status != BulkEmailJobStatus.Resolving)
            throw new BusinessRuleViolationException(Errors.InvalidTransition(Status, BulkEmailJobStatus.Sending));

        _recipients.Clear();
        _recipients.AddRange(recipients);
        RecipientCount = _recipients.Count;
        SentCount = 0;
        FailedCount = 0;
        CancelledCount = 0;

        Status = BulkEmailJobStatus.Sending;
    }

    public void RecordSentRecipient(string email)
    {
        EnsureStatus(BulkEmailJobStatus.Sending);

        var recipient = FindPendingRecipient(email);
        recipient.MarkSent();
        SentCount++;
    }

    public void RecordFailedRecipient(string email, string error)
    {
        EnsureStatus(BulkEmailJobStatus.Sending);

        var recipient = FindPendingRecipient(email);
        recipient.MarkFailed(error);
        FailedCount++;
        LastError = error;
    }

    /// <summary>
    /// Cooperative-cancellation request. Valid in any non-terminal state.
    /// Idempotent: a second request is a no-op.
    /// </summary>
    public void RequestCancellation(DateTimeOffset now)
    {
        if (IsTerminal)
            throw new BusinessRuleViolationException(Errors.AlreadyTerminal(Status));

        CancellationRequestedAt ??= now;
    }

    /// <summary>
    /// Worker-side finalisation of a cancellation: marks remaining
    /// <see cref="BulkEmailRecipientStatus.Pending"/> recipients as
    /// <see cref="BulkEmailRecipientStatus.Cancelled"/> and transitions the
    /// job to <see cref="BulkEmailJobStatus.Cancelled"/>.
    /// </summary>
    public void FinaliseCancelled(DateTimeOffset now)
    {
        if (CancellationRequestedAt is null)
            throw new BusinessRuleViolationException(Errors.NoCancellationRequested);

        if (IsTerminal)
            return;

        foreach (var recipient in _recipients.Where(r => r.Status == BulkEmailRecipientStatus.Pending))
        {
            recipient.MarkCancelled();
            CancelledCount++;
        }

        Status = BulkEmailJobStatus.Cancelled;
        CancelledAt = now;
        CompletedAt = now;
    }

    /// <summary>
    /// Worker-side finalisation when the snapshot has been fully processed.
    /// Picks the appropriate terminal state based on per-recipient outcomes.
    /// </summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status != BulkEmailJobStatus.Resolving && Status != BulkEmailJobStatus.Sending)
            throw new BusinessRuleViolationException(Errors.InvalidTransition(Status, BulkEmailJobStatus.Completed));

        if (RecipientCount == 0)
        {
            Status = BulkEmailJobStatus.Completed;
        }
        else if (FailedCount == RecipientCount)
        {
            Status = BulkEmailJobStatus.Failed;
        }
        else if (FailedCount > 0)
        {
            Status = BulkEmailJobStatus.PartiallyFailed;
        }
        else
        {
            Status = BulkEmailJobStatus.Completed;
        }

        CompletedAt = now;
    }

    /// <summary>
    /// Marks the job as terminally failed (e.g. resolution failed before any
    /// recipient was processed). Use <see cref="Complete"/> for normal end-of-fan-out.
    /// </summary>
    public void Fail(string error, DateTimeOffset now)
    {
        if (IsTerminal)
            throw new BusinessRuleViolationException(Errors.AlreadyTerminal(Status));

        Status = BulkEmailJobStatus.Failed;
        LastError = error;
        CompletedAt = now;
    }

    private bool IsTerminal => Status is BulkEmailJobStatus.Completed
        or BulkEmailJobStatus.PartiallyFailed
        or BulkEmailJobStatus.Failed
        or BulkEmailJobStatus.Cancelled;

    private void EnsureStatus(BulkEmailJobStatus expected)
    {
        if (Status != expected)
            throw new BusinessRuleViolationException(Errors.InvalidStateForOperation(Status, expected));
    }

    private BulkEmailRecipient FindPendingRecipient(string email)
    {
        var recipient = _recipients.FirstOrDefault(r =>
            string.Equals(r.Email, email, StringComparison.OrdinalIgnoreCase));

        if (recipient is null)
            throw new BusinessRuleViolationException(Errors.RecipientNotFound(email));

        if (recipient.Status != BulkEmailRecipientStatus.Pending)
            throw new BusinessRuleViolationException(Errors.RecipientNotPending(email, recipient.Status));

        return recipient;
    }

    internal static class Errors
    {
        public static Error InvalidTransition(BulkEmailJobStatus from, BulkEmailJobStatus to) =>
            new(
                "bulk_email_job.invalid_transition",
                $"Cannot transition bulk-email job from {from} to {to}.",
                Type: ErrorType.Conflict);

        public static Error InvalidStateForOperation(BulkEmailJobStatus current, BulkEmailJobStatus expected) =>
            new(
                "bulk_email_job.invalid_state",
                $"Operation requires status {expected}, but the job is {current}.",
                Type: ErrorType.Conflict);

        public static Error AlreadyTerminal(BulkEmailJobStatus status) =>
            new(
                "bulk_email_job.already_terminal",
                $"Bulk-email job is already in terminal state {status}.",
                Type: ErrorType.Conflict);

        public static readonly Error NoCancellationRequested = new(
            "bulk_email_job.no_cancellation_requested",
            "FinaliseCancelled requires a prior RequestCancellation call.",
            Type: ErrorType.Conflict);

        public static Error RecipientNotFound(string email) =>
            new(
                "bulk_email_job.recipient_not_found",
                $"Recipient '{email}' is not in the snapshot.",
                Type: ErrorType.Validation);

        public static Error RecipientNotPending(string email, BulkEmailRecipientStatus status) =>
            new(
                "bulk_email_job.recipient_not_pending",
                $"Recipient '{email}' is in status {status}; expected Pending.",
                Type: ErrorType.Conflict);
    }
}
