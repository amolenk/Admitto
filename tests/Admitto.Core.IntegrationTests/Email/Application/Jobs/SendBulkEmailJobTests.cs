using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
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
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Quartz;

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
    private const string DefaultEmailType = BuiltInEmailTemplateNames.Reconfirmation;

    [TestMethod]
    public async ValueTask Execute_AllRecipientsSucceed_CompletesUsingSingleSmtpSession()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com", "Alice"), Recipient("bob@example.com", "Bob")]);

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
    }

    [TestMethod]
    public async ValueTask Execute_ProjectedTeamName_OpensBulkSmtpSessionWithTeamDisplayName()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com", "Alice")],
            teamName: "Acme Events",
            replyToEmailAddress: "help@example.com");

        await fanOut.Execute(JobContext(job));

        fakeSender.LastOpenedSettings.ShouldNotBeNull();
        fakeSender.LastOpenedSettings.FromAddress.Value.ShouldBe("tickets@admitto.org");
        fakeSender.LastOpenedSettings.FromDisplayName.ShouldBe("Acme Events");
        fakeSender.LastOpenedSettings.ReplyToAddress.ShouldBe(EmailAddress.From("help@example.com"));
    }

    [TestMethod]
    public async ValueTask Execute_AllRecipientsFail_TransitionsToFailed()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com"), Recipient("bob@example.com")]);
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

    [TestMethod]
    public async ValueTask Execute_BeforeSmtpSend_WritesPendingEmailLog()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com")]);

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


    [TestMethod]
    public async ValueTask Execute_SomeRecipientsFail_TransitionsToPartiallyFailed()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com"), Recipient("bob@example.com")]);
        fakeSender.FailOn("bob@example.com");

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.PartiallyFailed);
        reloaded.SentCount.ShouldBe(1);
        reloaded.FailedCount.ShouldBe(1);
    }

    [TestMethod]
    public async ValueTask Execute_EmptyRecipientSet_CompletesImmediately()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(recipients: []);

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Completed);
        reloaded.RecipientCount.ShouldBe(0);

        fakeSender.SessionsOpened.ShouldBe(0);
        fakeSender.SentMessages.ShouldBeEmpty();
    }

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
            .Build();
        job.BeginResolving(DateTimeOffset.UtcNow);
        job.BeginSending([alice, bob]);
        job.RecordSentRecipient("alice@example.com");

        await Environment.EmailDatabase.SeedAsync(db => db.BulkEmailJobs.Add(job));

        var fakeSender = new FakeBulkSmtpSender();
        var fanOut = BuildFanOut(fakeSender, recipientResolver: NeverCalledResolver());

        // Act
        await fanOut.Execute(JobContext(job));

        // Assert: only Bob was sent on the resume pickup; Alice was already Sent.
        fakeSender.SentMessages.Count.ShouldBe(1);
        fakeSender.SentMessages[0].RecipientAddress.ShouldBe("bob@example.com");

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Completed);
        reloaded.SentCount.ShouldBe(2);
    }

    [TestMethod]
    public async ValueTask Execute_PreExistingEmailLogRow_DedupsViaUniqueIndex()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com")]);

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

    [TestMethod]
    public async ValueTask Execute_PreExistingFailedEmailLogRow_RecordsFailedRecipientWithoutSmtp()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com")]);

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

    [TestMethod]
    public async ValueTask Execute_TransientRecipientFailure_RetriesInlineBeforeRecordingFailure()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com")],
            inlineRetryCount: 2);
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

    [TestMethod]
    public async ValueTask Execute_CancellationRequestedBeforePickup_FinalisesCancelled()
    {
        var (job, fakeSender, fanOut) = await SetupAsync(
            recipients: [Recipient("alice@example.com")]);

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
            .Build();
        job.BeginResolving(DateTimeOffset.UtcNow);
        job.BeginSending([alice, bob]);
        job.RecordSentRecipient("alice@example.com");
        job.RequestCancellation(DateTimeOffset.UtcNow);

        await Environment.EmailDatabase.SeedAsync(db => db.BulkEmailJobs.Add(job));

        var fakeSender = new FakeBulkSmtpSender();
        var fanOut = BuildFanOut(fakeSender, recipientResolver: NeverCalledResolver());

        await fanOut.Execute(JobContext(job));

        var reloaded = await ReloadJobAsync(job.Id);
        reloaded.Status.ShouldBe(BulkEmailJobStatus.Cancelled);
        reloaded.SentCount.ShouldBe(1);
        reloaded.CancelledCount.ShouldBe(1);
        reloaded.Recipients.Single(r => r.Email == "bob@example.com").Status
            .ShouldBe(BulkEmailRecipientStatus.Cancelled);

        fakeSender.SentMessages.ShouldBeEmpty();
    }

    // --- helpers --------------------------------------------------------

    private static BulkEmailRecipient Recipient(string email, string? name = null) =>
        BulkEmailJobBuilder.Recipient(email, name);

    private async ValueTask<(BulkEmailJob Job, FakeBulkSmtpSender Sender, SendBulkEmailJob FanOut)> SetupAsync(
        IReadOnlyList<BulkEmailRecipient> recipients,
        TimeSpan? perMessageDelay = null,
        int? inlineRetryCount = null,
        string? teamName = null,
        string? replyToEmailAddress = null)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var job = new BulkEmailJobBuilder()
            .ForTeam(teamId).ForEvent(eventId)
            .WithEmailType(DefaultEmailType)
            .Build();
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.BulkEmailJobs.Add(job);

            if (teamName is not null)
            {
                var teamContext = TeamEmailContextView.CreatePartial(teamId, DateTimeOffset.UtcNow);
                teamContext.UpdateTeamContext(teamName, "#0f766e", replyToEmailAddress, teamVersion: 1, DateTimeOffset.UtcNow);
                db.TeamEmailContexts.Add(teamContext);
            }
        });

        var sender = new FakeBulkSmtpSender();
        var resolver = Substitute.For<IBulkEmailRecipientResolver>();
        resolver.ResolveAsync(teamId, eventId, Arg.Any<BulkEmailJobSource>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(recipients));

        var fanOut = BuildFanOut(sender, resolver, perMessageDelay ?? TimeSpan.Zero, inlineRetryCount);
        return (job, sender, fanOut);
    }

    private static SendBulkEmailJob BuildFanOut(
        FakeBulkSmtpSender sender,
        IBulkEmailRecipientResolver? recipientResolver = null,
        TimeSpan? perMessageDelay = null,
        int? inlineRetryCount = null)
    {
        var ctx = Environment.EmailDatabase.Context;

        IEmailWriteStore writeStore = ctx;
        var settingsResolver = new EffectiveEmailSettingsResolver(Options.Create(new SystemEmailOptions
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            FromAddress = "tickets@admitto.org",
            AuthMode = "None"
        }), ctx);
        var templateService = new EmailTemplateService();
        var renderer = new ScribanEmailRenderer();
        var eventContextQuery = Substitute.For<IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>>();
        eventContextQuery.HandleAsync(Arg.Any<GetEventEmailRenderingContextQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<GetEventEmailRenderingContextQuery>();
                return new EventEmailContextDto(
                    query.TeamId.Value,
                    query.TicketedEventId.Value,
                    "DevConf",
                    "https://example.com",
                    "https://tickets.example.com/e/devconf",
                    "https://tickets.example.com/e/devconf/register",
                    "https://tickets.example.com/e/devconf/qr-code",
                    "https://tickets.example.com/e/devconf/cancel",
                    "#0f766e",
                    null,
                    "UTC",
                    null,
                    null,
                    null,
                    null,
                    false);
            });
        IUnitOfWork unitOfWork = new UnitOfWork<EmailDbContext>(ctx, new NoOpOutboxMessageSender(), NullLogger<UnitOfWork<EmailDbContext>>.Instance);

        var options = new BulkEmailOptions
        {
            PerMessageDelay = perMessageDelay ?? TimeSpan.Zero,
            InlineRetryCount = inlineRetryCount ?? new BulkEmailOptions().InlineRetryCount,
            InlineRetryDelay = TimeSpan.Zero
        };
        var monitor = new StaticOptionsMonitor<BulkEmailOptions>(options);

        return new SendBulkEmailJob(
            writeStore,
            recipientResolver ?? Substitute.For<IBulkEmailRecipientResolver>(),
            eventContextQuery,
            settingsResolver,
            templateService,
            renderer,
            sender,
            unitOfWork,
            monitor,
            NullLogger<SendBulkEmailJob>.Instance);
    }

    private static IBulkEmailRecipientResolver NeverCalledResolver()
    {
        var resolver = Substitute.For<IBulkEmailRecipientResolver>();
        resolver
            .WhenForAnyArgs(r => r.ResolveAsync(TeamId.New(), TicketedEventId.New(), default!, default))
            .Do(_ => throw new InvalidOperationException("Resolver should not be called when resuming an in-flight job."));
        return resolver;
    }

    private static IJobExecutionContext JobContext(BulkEmailJob job)
    {
        var data = new JobDataMap();
        data[SendBulkEmailJob.BulkEmailJobIdKey] = job.Id.Value.ToString();
        data[SendBulkEmailJob.TeamIdKey] = job.TeamId.Value.ToString();
        data[SendBulkEmailJob.TicketedEventIdKey] = job.TicketedEventId.Value.ToString();

        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(data);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private async ValueTask<BulkEmailJob> ReloadJobAsync(BulkEmailJobId jobId)
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await Environment.EmailDatabase.Context.BulkEmailJobs
            .FirstAsync(j => j.Id == jobId, testContext.CancellationToken);
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
