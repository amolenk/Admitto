using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Domain.Tests.Entities;

[TestClass]
public sealed class ReconfirmationBatchTests
{
    private static readonly DateTimeOffset Now = new(2030, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // Given a newly-created reconfirmation batch
    // When it is inspected
    // Then it is pending with no delivery timestamps
    [TestMethod]
    public void Create_StartsPendingWithMinimalLifecycleState()
    {
        var batch = ReconfirmationBatch.Create(TeamId.New(), TicketedEventId.New(), Now);

        batch.Status.ShouldBe(ReconfirmationBatchStatus.Pending);
        batch.IsActive.ShouldBeTrue();
        batch.CreatedAt.ShouldBe(Now);
        batch.StartedAt.ShouldBeNull();
        batch.CompletedAt.ShouldBeNull();
        batch.LastError.ShouldBeNull();
    }

    // Given a pending reconfirmation batch
    // When sending begins
    // Then it becomes active and records its start time
    [TestMethod]
    public void BeginSending_FromPending_RecordsStart()
    {
        var batch = ReconfirmationBatch.Create(TeamId.New(), TicketedEventId.New(), Now);

        batch.BeginSending(Now.AddMinutes(1));

        batch.Status.ShouldBe(ReconfirmationBatchStatus.Sending);
        batch.StartedAt.ShouldBe(Now.AddMinutes(1));
        batch.IsActive.ShouldBeTrue();
    }

    // Given a reconfirmation batch that is sending
    // When delivery completes
    // Then it becomes terminally completed
    [TestMethod]
    public void Complete_FromSending_EndsLifecycle()
    {
        var batch = SendingBatch();

        batch.Complete(Now.AddMinutes(5));

        batch.Status.ShouldBe(ReconfirmationBatchStatus.Completed);
        batch.CompletedAt.ShouldBe(Now.AddMinutes(5));
        batch.IsActive.ShouldBeFalse();
    }

    // Given a reconfirmation batch that is sending
    // When the worker is interrupted
    // Then the batch is terminally failed without recipient state
    [TestMethod]
    public void Fail_FromSending_EndsAsFailedWithError()
    {
        var batch = SendingBatch();

        batch.Fail("worker stopped", Now.AddMinutes(5));

        batch.Status.ShouldBe(ReconfirmationBatchStatus.Failed);
        batch.CompletedAt.ShouldBe(Now.AddMinutes(5));
        batch.LastError.ShouldBe("worker stopped");
        batch.IsActive.ShouldBeFalse();
    }

    // Given a completed reconfirmation batch
    // When a later interruption is recorded
    // Then the terminal result is preserved
    [TestMethod]
    public void Fail_FromTerminal_IsIdempotent()
    {
        var batch = SendingBatch();
        batch.Complete(Now.AddMinutes(5));

        batch.Fail("late failure", Now.AddMinutes(6));

        batch.Status.ShouldBe(ReconfirmationBatchStatus.Completed);
        batch.LastError.ShouldBeNull();
        batch.CompletedAt.ShouldBe(Now.AddMinutes(5));
    }

    private static ReconfirmationBatch SendingBatch()
    {
        var batch = ReconfirmationBatch.Create(TeamId.New(), TicketedEventId.New(), Now);
        batch.BeginSending(Now.AddMinutes(1));
        return batch;
    }
}
