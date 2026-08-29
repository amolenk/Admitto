using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Domain.Entities;

/// <summary>
/// The durable lifecycle record for one live reconfirmation evaluation.
/// Recipient state deliberately remains outside this aggregate: every new run
/// queries Registrations and uses EmailLog history rather than resuming work.
/// </summary>
public sealed class ReconfirmationBatch : Aggregate<ReconfirmationBatchId>
{
    private ReconfirmationBatch()
    {
    }

    private ReconfirmationBatch(
        ReconfirmationBatchId id,
        TeamId teamId,
        TicketedEventId ticketedEventId,
        DateTimeOffset createdAt)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        Status = ReconfirmationBatchStatus.Pending;
        CreatedAt = createdAt;
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId TicketedEventId { get; private set; }
    public ReconfirmationBatchStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? LastError { get; private set; }

    public bool IsActive => Status is ReconfirmationBatchStatus.Pending or ReconfirmationBatchStatus.Sending;

    public static ReconfirmationBatch Create(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        DateTimeOffset now) =>
        new(ReconfirmationBatchId.New(), teamId, ticketedEventId, now);

    public void BeginSending(DateTimeOffset now)
    {
        if (Status != ReconfirmationBatchStatus.Pending)
            throw new BusinessRuleViolationException(Errors.InvalidTransition(Status, ReconfirmationBatchStatus.Sending));

        Status = ReconfirmationBatchStatus.Sending;
        StartedAt ??= now;
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != ReconfirmationBatchStatus.Sending)
            throw new BusinessRuleViolationException(Errors.InvalidTransition(Status, ReconfirmationBatchStatus.Completed));

        Status = ReconfirmationBatchStatus.Completed;
        CompletedAt = now;
    }

    public void Fail(string error, DateTimeOffset now)
    {
        if (!IsActive)
            return;

        Status = ReconfirmationBatchStatus.Failed;
        LastError = error;
        CompletedAt = now;
    }

    internal static class Errors
    {
        public static Error InvalidTransition(
            ReconfirmationBatchStatus from,
            ReconfirmationBatchStatus to) =>
            new(
                "reconfirmation_batch.invalid_transition",
                $"Cannot transition reconfirmation batch from {from} to {to}.",
                Type: ErrorType.Conflict);
    }
}
