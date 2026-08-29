using System.Text.Json;
using System.Diagnostics;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs.Fakes;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Domain;
using Amolenk.Admitto.Testing.Builders.Email.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quartz;
using static Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs.SendBulkEmailJobFixture;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

/// <summary>
/// Integration tests for <see cref="SendBulkEmailJob"/> covering the
/// happy path, partial/total failure, resume-after-crash, idempotency-key
/// dedup, empty recipient sets, and cooperative cancellation.
/// Wires the real <see cref="EmailDbContext"/> from the Aspire-backed
/// integration test environment with fakes for SMTP and the recipient
/// resolver.
/// </summary>
[TestClass]
public sealed class SendBulkEmailJobTests(TestContext testContext) : AspireIntegrationTestBase
{
    private const string DefaultEmailType = BuiltInEmailTemplateNames.BulkCustom;

    // Given a bulk email job with two recipients that both succeed
    // When the job is executed
    // Then the job completes, both recipients are sent, and a single SMTP session is opened and closed
    [TestMethod]
    public async ValueTask Execute_AllRecipientsSucceed_CompletesUsingSingleSmtpSession()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Standard([Recipient("alice@example.com", "Alice"), Recipient("bob@example.com", "Bob")])
            .SetupAsync(Environment);

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Completed);
        reloaded.SentCount.ShouldBe(2);
        reloaded.FailedCount.ShouldBe(0);
        reloaded.Recipients.ShouldAllBe(r => r.Status == BulkEmailRecipientStatus.Sent);

        fakeSender.SessionsOpened.ShouldBe(1);
        fakeSender.SessionsClosed.ShouldBe(1);
        fakeSender.SentMessages.Count.ShouldBe(2);

        var logs = await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().ToListAsync(testContext.CancellationToken);
        logs.Count.ShouldBe(2);
        logs.ShouldAllBe(l => l.Status == EmailLogStatus.Sent && l.BulkEmailJobId == job.Id);
        logs.All(l => l.RegistrationCycleId is not null).ShouldBeTrue();
    }

    // Given a bulk email job for a team with a custom team name
    // When the job is executed
    // Then the SMTP session is opened using the platform's configured sender address and display name, not the team's
    [TestMethod]
    public async ValueTask Execute_OpensBulkSmtpSessionWithConfiguredPlatformSender()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .PlatformSender([Recipient("alice@example.com", "Alice")], "Acme Events")
            .SetupAsync(Environment);

        await fanOut.Execute(JobContext(job));

        // Sender identity is deployment configuration and never derived from the team.
        fakeSender.LastOpenedSettings.ShouldNotBeNull();
        fakeSender.LastOpenedSettings.FromAddress.Value.ShouldBe("tickets@admitto.org");
        fakeSender.LastOpenedSettings.FromDisplayName.ShouldBe("Admitto");
    }

    // Given a bulk custom email whose content references the team name
    // When the job is executed
    // Then the sent message's subject and body render the projected team name
    [TestMethod]
    public async ValueTask Execute_CustomContentWithTeamName_RendersProjectedTeamName()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture.CustomContent(
            [Recipient("alice@example.com", "Alice")],
                BuiltInEmailTemplateNames.BulkCustom,
                "Update from {{ team_name }}",
                "Regards, {{ team_name }}",
                "<p>Regards, {{ team_name }}</p>").SetupAsync(Environment);

        await fanOut.Execute(JobContext(job));

        var message = fakeSender.SentMessages.Single();
        message.Subject.ShouldBe("Update from DevConf Team");
        message.TextBody.ShouldBe("Regards, DevConf Team");
        message.HtmlBody.ShouldBe("<p>Regards, DevConf Team</p>");
    }

    // Given a bulk custom email whose content references branding and QR code parameters
    // When the job is executed
    // Then the sent message renders the canonical parameter names and leaves unrecognized aliases empty
    [TestMethod]
    public async ValueTask Execute_CustomContentWithBrandingAndQrCode_RendersCanonicalParameters()
    {
        var recipient = Recipient("alice@example.com", "Alice");
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture.CustomContent(
                [recipient],
                BuiltInEmailTemplateNames.BulkCustom,
                "{{ accent_color }} {{ team_accent_color }}",
                "{{ qrcode_link }}",
                "{{ qr_code_link }}").SetupAsync(Environment);

        await fanOut.Execute(JobContext(job));

        var message = fakeSender.SentMessages.Single();
        message.Subject.ShouldBe("#0f766e ");
        message.TextBody.ShouldBe($"https://tickets.example.com/e/devconf/qr-code/{recipient.RegistrationId.Value}");
        message.HtmlBody.ShouldBeEmpty();
    }

    // Given a bulk email job with two recipients that both fail to send
    // When the job is executed
    // Then the job transitions to Failed with no sent messages and both recipients logged as failed
    [TestMethod]
    public async ValueTask Execute_AllRecipientsFail_TransitionsToFailed()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Standard([Recipient("alice@example.com"), Recipient("bob@example.com")])
            .SetupAsync(Environment);
        fakeSender.FailOn("alice@example.com");
        fakeSender.FailOn("bob@example.com");

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Failed);
        reloaded.FailedCount.ShouldBe(2);
        reloaded.SentCount.ShouldBe(0);

        fakeSender.SentMessages.ShouldBeEmpty();

        var logs = await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().ToListAsync(testContext.CancellationToken);
        logs.ShouldAllBe(l => l.Status == EmailLogStatus.Failed);
        logs.Count.ShouldBe(2);
    }

    // Given a bulk email job with one recipient
    // When the job is executed
    // Then an email log row exists in Pending status before the SMTP send occurs
    [TestMethod]
    public async ValueTask Execute_BeforeSmtpSend_WritesPendingEmailLog()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Standard([Recipient("alice@example.com")])
            .SetupAsync(Environment);

        var sawPendingLog = false;
        fakeSender.OnBeforeSendAsync = async message =>
        {
            var idempotencyKey = $"bulk:{job.Id.Value:N}:{message.RecipientAddress.ToLowerInvariant()}";
            var log = await Environment.EmailDatabase.Context.EmailLog
                .AsNoTracking()
                .SingleAsync(l => l.IdempotencyKey == idempotencyKey, testContext.CancellationToken);

            log.Status.ShouldBe(EmailLogStatus.Pending);
            sawPendingLog = true;
        };

        await fanOut.Execute(JobContext(job));

        sawPendingLog.ShouldBeTrue();
        fakeSender.SentMessages.Count.ShouldBe(1);
    }


    // Given a bulk email job with two recipients where one fails to send
    // When the job is executed
    // Then the job transitions to PartiallyFailed with one sent and one failed recipient
    [TestMethod]
    public async ValueTask Execute_SomeRecipientsFail_TransitionsToPartiallyFailed()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Standard([Recipient("alice@example.com"), Recipient("bob@example.com")])
            .SetupAsync(Environment);
        fakeSender.FailOn("bob@example.com");

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.PartiallyFailed);
        reloaded.SentCount.ShouldBe(1);
        reloaded.FailedCount.ShouldBe(1);
    }

    // Given a bulk email job with no recipients
    // When the job is executed
    // Then the job completes immediately without opening an SMTP session or sending any messages
    [TestMethod]
    public async ValueTask Execute_EmptyRecipientSet_CompletesImmediately()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Standard([])
            .SetupAsync(Environment);

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Completed);
        reloaded.RecipientCount.ShouldBe(0);

        fakeSender.SessionsOpened.ShouldBe(0);
        fakeSender.SentMessages.ShouldBeEmpty();
    }

    // Given a job already in Sending status from a crashed pickup, with one recipient Sent and one Pending
    // When the job is executed again (a resume pickup)
    // Then only the still-pending recipient is sent and the job completes
    [TestMethod]
    public async ValueTask Execute_ResumeAfterCrash_OnlyProcessesPendingRecipients()
    {
        // Arrange: simulate a previous crashed pickup by seeding a job already in
        // Sending status with one recipient marked Sent and one still Pending.
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var alice = BulkEmailJobBuilder.Recipient("alice@example.com", "Alice");
        var bob = BulkEmailJobBuilder.Recipient("bob@example.com", "Bob");

        var job = new BulkEmailJobBuilder()
            .ForTeam(teamId).ForEvent(eventId)
            .WithEmailType(DefaultEmailType)
            .WithAdHocBodies("Bulk update", "Bulk update", "<p>Bulk update</p>")
            .Build();
        job.BeginResolving(DateTimeOffset.UtcNow);
        job.BeginSending([alice, bob]);
        job.RecordSentRecipient("alice@example.com");

        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.BulkEmailJobs.Add(job);
            db.TeamEmailContexts.Add(CreateTeamEmailContext(teamId));
            db.EventEmailContexts.Add(CreateEventEmailContext(teamId, eventId, DateTimeOffset.UtcNow));
        });

        var fakeSender = new FakeBulkSmtpSender();
        var fanOut = SendBulkEmailJobFixture.BuildExistingJobFanOut(
            Environment,
            fakeSender,
            NeverCalledResolver(),
            CurrentRegistrationsFacade([alice, bob]));

        // Act
        await fanOut.Execute(JobContext(job));

        // Assert: only Bob was sent on the resume pickup; Alice was already Sent.
        fakeSender.SentMessages.Count.ShouldBe(1);
        fakeSender.SentMessages[0].RecipientAddress.ShouldBe("bob@example.com");

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Completed);
        reloaded.SentCount.ShouldBe(2);
    }

    // Given an email log row already recorded as Sent for the recipient's idempotency key
    // When the job is executed
    // Then no message is sent again and the job still completes with the recipient counted as sent
    [TestMethod]
    public async ValueTask Execute_PreExistingEmailLogRow_DedupsViaUniqueIndex()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Standard([Recipient("alice@example.com")])
            .SetupAsync(Environment);

        var idempotencyKey = $"bulk:{job.Id.Value:N}:alice@example.com";
        var preExisting = EmailLog.Create(
            teamId: job.TeamId,
            ticketedEventId: job.TicketedEventId,
            idempotencyKey: idempotencyKey,
            recipient: EmailAddress.From("alice@example.com"),
            emailType: DefaultEmailType,
            subject: "Pre-existing",
            status: EmailLogStatus.Sent,
            sentAt: DateTimeOffset.UtcNow,
            statusUpdatedAt: DateTimeOffset.UtcNow,
            bulkEmailJobId: job.Id);
        await Environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(preExisting));

        await fanOut.Execute(JobContext(job));

        fakeSender.SentMessages.ShouldBeEmpty();

        var logs = await Environment.EmailDatabase.Context.EmailLog.AsNoTracking()
            .Where(l => l.IdempotencyKey == idempotencyKey)
            .ToListAsync(testContext.CancellationToken);
        logs.Count.ShouldBe(1);

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Completed);
        reloaded.SentCount.ShouldBe(1);
    }

    // Given an email log row already recorded as Failed for the recipient's idempotency key
    // When the job is executed
    // Then the recipient is recorded as failed with the prior error, without attempting SMTP again
    [TestMethod]
    public async ValueTask Execute_PreExistingFailedEmailLogRow_RecordsFailedRecipientWithoutSmtp()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Standard([Recipient("alice@example.com")])
            .SetupAsync(Environment);

        var idempotencyKey = $"bulk:{job.Id.Value:N}:alice@example.com";
        var preExisting = EmailLog.Create(
            teamId: job.TeamId,
            ticketedEventId: job.TicketedEventId,
            idempotencyKey: idempotencyKey,
            recipient: EmailAddress.From("alice@example.com"),
            emailType: DefaultEmailType,
            subject: "Pre-existing",
            status: EmailLogStatus.Failed,
            sentAt: null,
            statusUpdatedAt: DateTimeOffset.UtcNow,
            lastError: "Previous deterministic failure.",
            bulkEmailJobId: job.Id);
        await Environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(preExisting));

        await fanOut.Execute(JobContext(job));

        fakeSender.SendAttempts.ShouldBeEmpty();

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Failed);
        reloaded.FailedCount.ShouldBe(1);
        reloaded.Recipients.Single().LastError.ShouldBe("Previous deterministic failure.");
    }























    // Given a recipient whose SMTP send always fails and inline retries are configured
    // When the job is executed
    // Then the send is retried inline the configured number of times before the recipient is recorded as failed
    [TestMethod]
    public async ValueTask Execute_TransientRecipientFailure_RetriesInlineBeforeRecordingFailure()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Retryable([Recipient("alice@example.com")], 2)
            .SetupAsync(Environment);
        fakeSender.FailOn("alice@example.com");

        await fanOut.Execute(JobContext(job));

        fakeSender.SendAttempts.Count(a => a == "alice@example.com").ShouldBe(3);

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Failed);
        reloaded.FailedCount.ShouldBe(1);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(l => l.BulkEmailJobId == job.Id, testContext.CancellationToken);
        log.Status.ShouldBe(EmailLogStatus.Failed);
        log.LastError.ShouldNotBeNull();
        log.LastError.ShouldContain("SMTP error (fake)");
    }

    // Given a generic bulk job configured with a per-recipient delay
    // When the fan-out sends two recipients
    // Then it honors the configured delay between sends
    [TestMethod]
    public async ValueTask Execute_ConfiguredGenericBulkDelay_IsHonored()
    {
        var configuredDelay = TimeSpan.FromMilliseconds(40);
        var (job, _, fanOut) = await SendBulkEmailJobFixture
            .Delayed([Recipient("alice@example.com"), Recipient("bob@example.com")], configuredDelay)
            .SetupAsync(Environment);
        var stopwatch = Stopwatch.StartNew();

        await fanOut.Execute(JobContext(job));

        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(configuredDelay);
    }

    // Given a job whose cancellation was requested before any pickup ran
    // When the job is executed
    // Then the job finalizes as Cancelled without opening an SMTP session or sending messages
    [TestMethod]
    public async ValueTask Execute_CancellationRequestedBeforePickup_FinalisesCancelled()
    {
        var (job, fakeSender, fanOut) = await SendBulkEmailJobFixture
            .Standard([Recipient("alice@example.com")])
            .SetupAsync(Environment);

        // Mark cancellation requested before any pickup runs.
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            var tracked = db.BulkEmailJobs.Single(j => j.Id == job.Id);
            tracked.RequestCancellation(DateTimeOffset.UtcNow);
        });

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Cancelled);
        fakeSender.SessionsOpened.ShouldBe(0);
        fakeSender.SentMessages.ShouldBeEmpty();
    }

    // Given a job already Sending (from a crashed pickup) with one recipient Sent and one Pending, and cancellation requested before the resume pickup
    // When the job is executed again
    // Then no further messages are sent and the job finalizes as Cancelled with the remaining recipient marked Cancelled
    [TestMethod]
    public async ValueTask Execute_CancellationRequestedDuringSending_RemainingRecipientsCancelled()
    {
        // Simulate "operator cancelled the job between pickups": the job is
        // already in Sending (from a prior crashed pickup) with one recipient
        // Sent and one Pending; CancellationRequestedAt is set before the
        // resume pickup runs. The fan-out should observe cancellation in the
        // per-recipient poll, send no further messages, and finalise Cancelled
        // with the Pending recipient transitioned to Cancelled.
        //
        // Note: triggering cancellation in-flight from the test would require
        // an external write to the BulkEmailJob row. That bumps xmin and
        // breaks the worker's optimistic-concurrency check on its tracked
        // aggregate. The pre-set scenario covers the same observable outcome
        // (Status=Cancelled, remaining recipients=Cancelled, no extra sends).
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var alice = BulkEmailJobBuilder.Recipient("alice@example.com", "Alice");
        var bob = BulkEmailJobBuilder.Recipient("bob@example.com", "Bob");

        var job = new BulkEmailJobBuilder()
            .ForTeam(teamId).ForEvent(eventId)
            .WithEmailType(DefaultEmailType)
            .WithAdHocBodies("Bulk update", "Bulk update", "<p>Bulk update</p>")
            .Build();
        job.BeginResolving(DateTimeOffset.UtcNow);
        job.BeginSending([alice, bob]);
        job.RecordSentRecipient("alice@example.com");
        job.RequestCancellation(DateTimeOffset.UtcNow);

        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.BulkEmailJobs.Add(job);
            db.TeamEmailContexts.Add(CreateTeamEmailContext(teamId));
            db.EventEmailContexts.Add(CreateEventEmailContext(teamId, eventId, DateTimeOffset.UtcNow));
        });

        var fakeSender = new FakeBulkSmtpSender();
        var fanOut = SendBulkEmailJobFixture.BuildExistingJobFanOut(
            Environment,
            fakeSender,
            NeverCalledResolver(),
            CurrentRegistrationsFacade([alice, bob]));

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Cancelled);
        reloaded.SentCount.ShouldBe(1);
        reloaded.CancelledCount.ShouldBe(1);
        reloaded.Recipients.Single(r => r.Email == "bob@example.com").Status
            .ShouldBe(BulkEmailRecipientStatus.Cancelled);

        fakeSender.SentMessages.ShouldBeEmpty();
    }

    private async ValueTask<BulkEmailJob> ReloadJobAsync(BulkEmailJobId jobId)
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await Environment.EmailDatabase.Context.BulkEmailJobs
            .FirstAsync(j => j.Id == jobId, testContext.CancellationToken);
    }

}
