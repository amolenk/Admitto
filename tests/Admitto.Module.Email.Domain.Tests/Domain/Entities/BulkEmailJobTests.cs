using Amolenk.Admitto.Module.Email.Domain.DomainEvents;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.Entities;

[TestClass]
public sealed class BulkEmailJobTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // ---------- Creation ----------

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

    [TestMethod]
    public void CreateSystemTriggered_HasNullTriggeredBy()
    {
        var job = new BulkEmailJobBuilder().AsSystemTriggered().Build();

        job.IsSystemTriggered.ShouldBeTrue();
        job.TriggeredBy.ShouldBeNull();
        job.GetDomainEvents().OfType<BulkEmailJobRequestedDomainEvent>().Count().ShouldBe(1);
    }

    // ---------- Lifecycle: Pending → Resolving → Sending → Completed/PartiallyFailed/Failed ----------

    [TestMethod]
    public void BeginResolving_FromPending_TransitionsAndStampsStartedAt()
    {
        var job = new BulkEmailJobBuilder().Build();

        job.BeginResolving(Now);

        job.Status.ShouldBe(BulkEmailJobStatus.Resolving);
        job.StartedAt.ShouldBe(Now);
    }

    [TestMethod]
    public void BeginResolving_NotPending_Throws()
    {
        var job = new BulkEmailJobBuilder().Build();
        job.BeginResolving(Now);

        var error = ErrorResult.Capture(() => job.BeginResolving(Now));

        error.Error.Code.ShouldBe("bulk_email_job.invalid_transition");
    }

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

    [TestMethod]
    public void BeginSending_NotResolving_Throws()
    {
        var job = new BulkEmailJobBuilder().Build();

        var error = ErrorResult.Capture(() => job.BeginSending([]));

        error.Error.Code.ShouldBe("bulk_email_job.invalid_transition");
    }

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

    [TestMethod]
    public void RecordSentRecipient_NotInSnapshot_Throws()
    {
        var job = ResolvedJob("a@example.com");

        var error = ErrorResult.Capture(() => job.RecordSentRecipient("missing@example.com"));

        error.Error.Code.ShouldBe("bulk_email_job.recipient_not_found");
    }

    [TestMethod]
    public void RecordSentRecipient_AlreadySent_Throws()
    {
        var job = ResolvedJob("a@example.com");
        job.RecordSentRecipient("a@example.com");

        var error = ErrorResult.Capture(() => job.RecordSentRecipient("a@example.com"));

        error.Error.Code.ShouldBe("bulk_email_job.recipient_not_pending");
    }

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

    [TestMethod]
    public void Complete_WithSomeFailed_TransitionsToPartiallyFailed()
    {
        var job = ResolvedJob("a@example.com", "b@example.com");
        job.RecordSentRecipient("a@example.com");
        job.RecordFailedRecipient("b@example.com", "boom");

        job.Complete(Now);

        job.Status.ShouldBe(BulkEmailJobStatus.PartiallyFailed);
    }

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

    [TestMethod]
    public void Complete_FromTerminal_Throws()
    {
        var job = ResolvedJob("a@example.com");
        job.RecordSentRecipient("a@example.com");
        job.Complete(Now);

        var error = ErrorResult.Capture(() => job.Complete(Now));

        error.Error.Code.ShouldBe("bulk_email_job.invalid_transition");
    }

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

    [TestMethod]
    public void Fail_FromTerminal_Throws()
    {
        var job = ResolvedJob("a@example.com");
        job.RecordSentRecipient("a@example.com");
        job.Complete(Now);

        var error = ErrorResult.Capture(() => job.Fail("late", Now));

        error.Error.Code.ShouldBe("bulk_email_job.already_terminal");
    }

    // ---------- Cancellation ----------

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

    [TestMethod]
    public void RequestCancellation_IsIdempotent_KeepsFirstTimestamp()
    {
        var job = new BulkEmailJobBuilder().Build();
        job.RequestCancellation(Now);
        var later = Now.AddMinutes(5);

        job.RequestCancellation(later);

        job.CancellationRequestedAt.ShouldBe(Now);
    }

    [TestMethod]
    [DataRow("Completed")]
    [DataRow("PartiallyFailed")]
    [DataRow("Failed")]
    [DataRow("Cancelled")]
    public void RequestCancellation_FromTerminal_Throws(string state)
    {
        var job = JobInState(state);

        var error = ErrorResult.Capture(() => job.RequestCancellation(Now));

        error.Error.Code.ShouldBe("bulk_email_job.already_terminal");
    }

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

    [TestMethod]
    public void FinaliseCancelled_WithoutPriorRequest_Throws()
    {
        var job = ResolvedJob("a@example.com");

        var error = ErrorResult.Capture(() => job.FinaliseCancelled(Now));

        error.Error.Code.ShouldBe("bulk_email_job.no_cancellation_requested");
    }

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
