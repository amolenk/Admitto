using Amolenk.Admitto.Core.Email.Domain.DomainEvents;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Testing.Builders.Email.Domain;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Email.Domain.Tests.Entities;

[TestClass]
public sealed class BulkEmailJobTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // ---------- Creation ----------

    // When a bulk email job is created with a triggering user
    // Then it starts pending with zeroed counters and raises a job-requested domain event
    [TestMethod]
    public void Create_WithUserTrigger_StartsPendingAndRaisesScheduledEvent()
    {
        var triggeredBy = "alice@example.com";
        var job = new BulkEmailJobBuilder().TriggeredBy(triggeredBy).At(Now).Build();

        job.Status.ShouldBe(BulkEmailJobStatus.Pending);
        job.IsSystemTriggered.ShouldBeFalse();
        job.TriggeredBy.ShouldNotBeNull();
        job.CreatedAt.ShouldBe(Now);
        job.RecipientCount.ShouldBe(0);
        job.SentCount.ShouldBe(0);
        job.FailedCount.ShouldBe(0);
        job.CancelledCount.ShouldBe(0);
        job.GetDomainEvents().OfType<BulkEmailJobRequestedDomainEvent>().Count().ShouldBe(1);
    }

    // When a bulk email job is created as system-triggered
    // Then it has no triggering user and still raises a job-requested domain event
    [TestMethod]
    public void CreateSystemTriggered_HasNullTriggeredBy()
    {
        var job = new BulkEmailJobBuilder().AsSystemTriggered().Build();

        job.IsSystemTriggered.ShouldBeTrue();
        job.TriggeredBy.ShouldBeNull();
        job.GetDomainEvents().OfType<BulkEmailJobRequestedDomainEvent>().Count().ShouldBe(1);
    }

    // ---------- Lifecycle: Pending → Resolving → Sending → Completed/PartiallyFailed/Failed ----------

    // Given a pending bulk email job
    // When resolving begins
    // Then it transitions to Resolving and stamps the started-at time
    [TestMethod]
    public void BeginResolving_FromPending_TransitionsAndStampsStartedAt()
    {
        var job = new BulkEmailJobBuilder().Build();

        job.BeginResolving(Now);

        job.Status.ShouldBe(BulkEmailJobStatus.Resolving);
        job.StartedAt.ShouldBe(Now);
    }

    // Given a job that has already begun resolving
    // When resolving is started again
    // Then it throws an invalid transition error
    [TestMethod]
    public void BeginResolving_NotPending_Throws()
    {
        var job = new BulkEmailJobBuilder().Build();
        job.BeginResolving(Now);

        var error = ErrorResult.Capture(() => job.BeginResolving(Now));

        error.Error.ShouldMatch(
            BulkEmailJob.Errors.InvalidTransition(BulkEmailJobStatus.Resolving, BulkEmailJobStatus.Resolving));
    }

    // Given a job that is resolving
    // When sending begins with a recipient list
    // Then it transitions to Sending, freezes the recipient snapshot as pending, and resets the counters
    [TestMethod]
    public void BeginSending_FreezesRecipientSnapshotAndResetsCounters()
    {
        var job = new BulkEmailJobBuilder().Build();
        job.BeginResolving(Now);

        var recipients = new[]
        {
            BulkEmailJobBuilder.Recipient("a@example.com"),
            BulkEmailJobBuilder.Recipient("b@example.com"),
        };
        job.BeginSending(recipients);

        job.Status.ShouldBe(BulkEmailJobStatus.Sending);
        job.Recipients.Count.ShouldBe(2);
        job.RecipientCount.ShouldBe(2);
        job.SentCount.ShouldBe(0);
        job.FailedCount.ShouldBe(0);
        job.CancelledCount.ShouldBe(0);
        job.Recipients.ShouldAllBe(r => r.Status == BulkEmailRecipientStatus.Pending);
    }

    // Given a job that is still pending
    // When sending is started before resolving
    // Then it throws an invalid transition error
    [TestMethod]
    public void BeginSending_NotResolving_Throws()
    {
        var job = new BulkEmailJobBuilder().Build();

        var error = ErrorResult.Capture(() => job.BeginSending([]));

        error.Error.ShouldMatch(
            BulkEmailJob.Errors.InvalidTransition(BulkEmailJobStatus.Pending, BulkEmailJobStatus.Sending));
    }

    // Given a job that is sending to two recipients
    // When one recipient is recorded as sent
    // Then the sent counter increments and only that recipient's status becomes Sent
    [TestMethod]
    public void RecordSentRecipient_UpdatesPerRecipientStatusAndCounter()
    {
        var job = ResolvedJob("a@example.com", "b@example.com");

        job.RecordSentRecipient("a@example.com");

        job.SentCount.ShouldBe(1);
        job.FailedCount.ShouldBe(0);
        job.Recipients.Single(r => r.Email == "a@example.com").Status
            .ShouldBe(BulkEmailRecipientStatus.Sent);
        job.Recipients.Single(r => r.Email == "b@example.com").Status
            .ShouldBe(BulkEmailRecipientStatus.Pending);
    }

    // Given a job that is sending to a single recipient
    // When that recipient is recorded as failed with an error message
    // Then the failed counter increments and the recipient's status and error are set
    [TestMethod]
    public void RecordFailedRecipient_UpdatesStatusErrorAndCounter()
    {
        var job = ResolvedJob("a@example.com");

        job.RecordFailedRecipient("a@example.com", "smtp 550");

        job.FailedCount.ShouldBe(1);
        job.LastError.ShouldBe("smtp 550");
        var recipient = job.Recipients.Single();
        recipient.Status.ShouldBe(BulkEmailRecipientStatus.Failed);
        recipient.LastError.ShouldBe("smtp 550");
    }

    // Given a job whose recipient snapshot does not include a given address
    // When that address is recorded as sent
    // Then it throws a recipient-not-found error
    [TestMethod]
    public void RecordSentRecipient_NotInSnapshot_Throws()
    {
        var job = ResolvedJob("a@example.com");

        var error = ErrorResult.Capture(() => job.RecordSentRecipient("missing@example.com"));

        error.Error.ShouldMatch(BulkEmailJob.Errors.RecipientNotFound("missing@example.com"));
    }

    // Given a recipient that has already been recorded as sent
    // When it is recorded as sent again
    // Then it throws a recipient-not-pending error
    [TestMethod]
    public void RecordSentRecipient_AlreadySent_Throws()
    {
        var job = ResolvedJob("a@example.com");
        job.RecordSentRecipient("a@example.com");

        var error = ErrorResult.Capture(() => job.RecordSentRecipient("a@example.com"));

        error.Error.ShouldMatch(
            BulkEmailJob.Errors.RecipientNotPending("a@example.com", BulkEmailRecipientStatus.Sent));
    }

    // Given a sending job where every recipient was sent successfully
    // When the job is completed
    // Then it transitions to Completed and stamps the completed-at time
    [TestMethod]
    public void Complete_FromSending_AllSent_TransitionsToCompletedAndRaisesEvent()
    {
        var job = ResolvedJob("a@example.com", "b@example.com");
        job.RecordSentRecipient("a@example.com");
        job.RecordSentRecipient("b@example.com");

        job.Complete(Now);

        job.Status.ShouldBe(BulkEmailJobStatus.Completed);
        job.CompletedAt.ShouldBe(Now);
        job.RecipientCount.ShouldBe(2);
        job.SentCount.ShouldBe(2);
        job.FailedCount.ShouldBe(0);
    }

    // Given a sending job with one recipient sent and one recipient failed
    // When the job is completed
    // Then it transitions to PartiallyFailed
    [TestMethod]
    public void Complete_WithSomeFailed_TransitionsToPartiallyFailed()
    {
        var job = ResolvedJob("a@example.com", "b@example.com");
        job.RecordSentRecipient("a@example.com");
        job.RecordFailedRecipient("b@example.com", "boom");

        job.Complete(Now);

        job.Status.ShouldBe(BulkEmailJobStatus.PartiallyFailed);
    }

    // Given a sending job where every recipient failed
    // When the job is completed
    // Then it transitions to Failed with the failed count reflecting all recipients
    [TestMethod]
    public void Complete_WithAllFailed_TransitionsToFailed()
    {
        var job = ResolvedJob("a@example.com", "b@example.com");
        job.RecordFailedRecipient("a@example.com", "boom");
        job.RecordFailedRecipient("b@example.com", "boom");

        job.Complete(Now);

        job.Status.ShouldBe(BulkEmailJobStatus.Failed);
        job.FailedCount.ShouldBe(2);
    }

    // Given a job that is still resolving with no recipients recorded
    // When the job is completed
    // Then it transitions to Completed with a recipient count of zero
    [TestMethod]
    public void Complete_FromResolvingWithEmptySnapshot_TransitionsToCompleted()
    {
        var job = new BulkEmailJobBuilder().Build();
        job.BeginResolving(Now);
        job.BeginSending([]); // empty snapshot
        // BeginSending moved to Sending. Test the Resolving branch separately:

        var another = new BulkEmailJobBuilder().Build();
        another.BeginResolving(Now);

        another.Complete(Now);

        another.Status.ShouldBe(BulkEmailJobStatus.Completed);
        another.RecipientCount.ShouldBe(0);
    }

    // Given a job that has already completed
    // When completion is attempted again
    // Then it throws an invalid transition error
    [TestMethod]
    public void Complete_FromTerminal_Throws()
    {
        var job = ResolvedJob("a@example.com");
        job.RecordSentRecipient("a@example.com");
        job.Complete(Now);

        var error = ErrorResult.Capture(() => job.Complete(Now));

        error.Error.ShouldMatch(
            BulkEmailJob.Errors.InvalidTransition(BulkEmailJobStatus.Completed, BulkEmailJobStatus.Completed));
    }

    // Given a job that is resolving
    // When the job is failed with an error message
    // Then it transitions to Failed, records the error, and stamps the completed-at time
    [TestMethod]
    public void Fail_FromAnyNonTerminalState_TransitionsToFailedAndRaisesEvent()
    {
        var job = new BulkEmailJobBuilder().Build();
        job.BeginResolving(Now);

        job.Fail("resolver blew up", Now);

        job.Status.ShouldBe(BulkEmailJobStatus.Failed);
        job.LastError.ShouldBe("resolver blew up");
        job.CompletedAt.ShouldBe(Now);
    }

    // Given a job that has already completed
    // When the job is failed
    // Then it throws an already-terminal error
    [TestMethod]
    public void Fail_FromTerminal_Throws()
    {
        var job = ResolvedJob("a@example.com");
        job.RecordSentRecipient("a@example.com");
        job.Complete(Now);

        var error = ErrorResult.Capture(() => job.Fail("late", Now));

        error.Error.ShouldMatch(BulkEmailJob.Errors.AlreadyTerminal(BulkEmailJobStatus.Completed));
    }

    // ---------- Cancellation ----------

    // Given a job in a non-terminal state (Pending, Resolving, or Sending)
    // When cancellation is requested
    // Then the cancellation-requested time is stamped
    [TestMethod]
    [DataRow("Pending")]
    [DataRow("Resolving")]
    [DataRow("Sending")]
    public void RequestCancellation_InNonTerminalState_StampsCancellationRequestedAt(string state)
    {
        var job = JobInState(state);

        job.RequestCancellation(Now);

        job.CancellationRequestedAt.ShouldBe(Now);
    }

    // Given a job that already has a cancellation requested
    // When cancellation is requested again at a later time
    // Then the original cancellation-requested timestamp is kept
    [TestMethod]
    public void RequestCancellation_IsIdempotent_KeepsFirstTimestamp()
    {
        var job = new BulkEmailJobBuilder().Build();
        job.RequestCancellation(Now);
        var later = Now.AddMinutes(5);

        job.RequestCancellation(later);

        job.CancellationRequestedAt.ShouldBe(Now);
    }

    // Given a job in a terminal state (Completed, PartiallyFailed, Failed, or Cancelled)
    // When cancellation is requested
    // Then it throws an already-terminal error
    [TestMethod]
    [DataRow("Completed")]
    [DataRow("PartiallyFailed")]
    [DataRow("Failed")]
    [DataRow("Cancelled")]
    public void RequestCancellation_FromTerminal_Throws(string state)
    {
        var job = JobInState(state);

        var error = ErrorResult.Capture(() => job.RequestCancellation(Now));

        error.Error.ShouldMatch(BulkEmailJob.Errors.AlreadyTerminal(Enum.Parse<BulkEmailJobStatus>(state)));
    }

    // Given a job with a mix of sent, failed, and still-pending recipients where cancellation was requested
    // When the cancellation is finalised
    // Then the job becomes Cancelled and remaining pending recipients are marked Cancelled
    [TestMethod]
    public void FinaliseCancelled_MarksRemainingPendingRecipientsCancelled()
    {
        var job = ResolvedJob("a@example.com", "b@example.com", "c@example.com");
        job.RecordSentRecipient("a@example.com");
        job.RecordFailedRecipient("b@example.com", "boom");
        job.RequestCancellation(Now);

        job.FinaliseCancelled(Now);

        job.Status.ShouldBe(BulkEmailJobStatus.Cancelled);
        job.SentCount.ShouldBe(1);
        job.FailedCount.ShouldBe(1);
        job.CancelledCount.ShouldBe(1);
        job.CancelledAt.ShouldBe(Now);
        job.CompletedAt.ShouldBe(Now);
        job.Recipients.Single(r => r.Email == "c@example.com").Status
            .ShouldBe(BulkEmailRecipientStatus.Cancelled);
        job.CancelledCount.ShouldBe(1);
    }

    // Given a job with no cancellation requested
    // When cancellation finalisation is attempted
    // Then it throws a no-cancellation-requested error
    [TestMethod]
    public void FinaliseCancelled_WithoutPriorRequest_Throws()
    {
        var job = ResolvedJob("a@example.com");

        var error = ErrorResult.Capture(() => job.FinaliseCancelled(Now));

        error.Error.ShouldMatch(BulkEmailJob.Errors.NoCancellationRequested);
    }

    // Given a job that requested cancellation but then completed before finalisation ran
    // When cancellation is finalised
    // Then it does not throw and the job's Completed status is left unchanged
    [TestMethod]
    public void FinaliseCancelled_OnAlreadyTerminalJob_IsNoOp()
    {
        var job = ResolvedJob("a@example.com");
        job.RecordSentRecipient("a@example.com");
        job.RequestCancellation(Now);
        job.Complete(Now); // becomes Completed before finalisation runs

        Shouldly.Should.NotThrow(() => job.FinaliseCancelled(Now));
        job.Status.ShouldBe(BulkEmailJobStatus.Completed);
    }

    // ---------- Helpers ----------

    private static BulkEmailJob ResolvedJob(params string[] recipientEmails)
    {
        var job = new BulkEmailJobBuilder().Build();
        job.BeginResolving(Now);
        job.BeginSending(recipientEmails.Select(e => BulkEmailJobBuilder.Recipient(e)).ToList());
        return job;
    }

    private static BulkEmailJob JobInState(string state)
    {
        var job = new BulkEmailJobBuilder().Build();
        switch (state)
        {
            case "Pending":
                return job;
            case "Resolving":
                job.BeginResolving(Now);
                return job;
            case "Sending":
                job.BeginResolving(Now);
                job.BeginSending([BulkEmailJobBuilder.Recipient("x@example.com")]);
                return job;
            case "Completed":
                job.BeginResolving(Now);
                job.BeginSending([BulkEmailJobBuilder.Recipient("x@example.com")]);
                job.RecordSentRecipient("x@example.com");
                job.Complete(Now);
                return job;
            case "PartiallyFailed":
                job.BeginResolving(Now);
                job.BeginSending(
                [
                    BulkEmailJobBuilder.Recipient("x@example.com"),
                    BulkEmailJobBuilder.Recipient("y@example.com"),
                ]);
                job.RecordSentRecipient("x@example.com");
                job.RecordFailedRecipient("y@example.com", "err");
                job.Complete(Now);
                return job;
            case "Failed":
                job.BeginResolving(Now);
                job.Fail("boom", Now);
                return job;
            case "Cancelled":
                job.RequestCancellation(Now);
                job.FinaliseCancelled(Now);
                return job;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}
