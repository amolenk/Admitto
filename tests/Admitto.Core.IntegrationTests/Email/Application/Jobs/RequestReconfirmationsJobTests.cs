using System.Text.Json;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs.Fakes;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

[TestClass]
public sealed class RequestReconfirmationsJobTests : AspireIntegrationTestBase
{
    private static readonly TeamId TeamId = TeamId.New();

    // Given an attendee who registered only 10 hours ago and a minimum email interval of 48 hours
    // When the reconfirmations job runs
    // Then no bulk reconfirm email job is created for them
    [TestMethod]
    public async ValueTask Execute_AttendeeRegisteredRecently_ExcludedFromBulkJob()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;

        var facade = FacadeReturning(eventId, [
            RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddHours(-10))
        ]);

        var job = BuildJob(facade, new FakeTimeProvider(now));
        await job.Execute(JobContext(eventId, minEmailIntervalHours: 48));

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
    }

    // Given an attendee who already received a reconfirmation email 10 hours ago and a minimum email interval of 48 hours
    // When the reconfirmations job runs
    // Then no bulk reconfirm email job is created for them because the interval hasn't elapsed
    [TestMethod]
    public async ValueTask Execute_AttendeeReceivedReconfirmRecently_ExcludedFromBulkJob()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;

        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", now.AddHours(-10))));

        var facade = FacadeReturning(eventId, [
            RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddHours(-72))
        ]);

        var job = BuildJob(facade, new FakeTimeProvider(now));
        await job.Execute(JobContext(eventId, minEmailIntervalHours: 48));

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
    }

    // Given an attendee whose last reconfirmation email was sent 72 hours ago and a minimum email interval of 48 hours
    // When the reconfirmations job runs
    // Then a bulk reconfirm email job is created that includes the attendee, and no auto-cancel event is published
    [TestMethod]
    public async ValueTask Execute_MinEmailIntervalElapsedSinceLastEmail_AttendeeIncluded()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var attendeeId = Guid.NewGuid();

        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", now.AddHours(-72))));

        var facade = FacadeReturning(eventId, [
            RegistrationItem(attendeeId, "alice@example.com", now.AddHours(-100))
        ]);

        var job = BuildJob(facade, new FakeTimeProvider(now));
        await job.Execute(JobContext(eventId, minEmailIntervalHours: 48));

        var jobs = await LoadBulkEmailJobsAsync();
        jobs.Count.ShouldBe(1);
        var filter = jobs[0].AttendeeFilter;
        filter.RegistrationIds.ShouldNotBeNull();
        filter.RegistrationIds.ShouldContain(attendeeId);
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

    // Given one attendee who already reached their maximum allowed reconfirm attempts and another with unlimited attempts and fewer emails sent
    // When the reconfirmations job runs
    // Then the unlimited attendee is included in the bulk reconfirm job while the maxed-out attendee triggers a reconfirm-auto-expired event instead
    [TestMethod]
    public async ValueTask Execute_MixedLogCounts_SplitsReconfirmAndAutoCancelSets()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var workshopId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "workshop@example.com", now.AddDays(-4)));
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "workshop@example.com", now.AddDays(-3)));
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "session@example.com", now.AddDays(-3)));
        });

        var facade = FacadeReturning(eventId, [
            RegistrationItem(workshopId, "workshop@example.com", now.AddDays(-10), effectiveMaxReconfirmAttempts: 2),
            RegistrationItem(sessionId, "session@example.com", now.AddDays(-10), effectiveMaxReconfirmAttempts: null)
        ]);

        var job = BuildJob(facade, new FakeTimeProvider(now));
        await job.Execute(JobContext(eventId, minEmailIntervalHours: 0));

        var bulkJobs = await LoadBulkEmailJobsAsync();
        bulkJobs.Count.ShouldBe(1);
        var filter = bulkJobs[0].AttendeeFilter;
        filter.RegistrationIds.ShouldBe([sessionId], ignoreOrder: true);

        var outboxMessages = await LoadOutboxMessagesAsync();
        outboxMessages.Count.ShouldBe(1);
        outboxMessages[0].Type.ShouldBe($"{nameof(Email)}:{nameof(ReconfirmAutoExpiredIntegrationEvent)}");
        GetRegistrationIds(outboxMessages[0].Payload).ShouldBe([workshopId], ignoreOrder: true);
    }

    // Given two attendees with no maximum reconfirm attempts configured, regardless of how many reconfirm emails they already received
    // When the reconfirmations job runs
    // Then both attendees are included in the bulk reconfirm job and no auto-cancel event is published
    [TestMethod]
    public async ValueTask Execute_NoEligibleTicketTypes_AllCandidatesGetReconfirmEmail()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "first@example.com", now.AddDays(-4)));
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "second@example.com", now.AddDays(-5)));
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "second@example.com", now.AddDays(-4)));
        });

        var facade = FacadeReturning(eventId, [
            RegistrationItem(firstId, "first@example.com", now.AddDays(-10), effectiveMaxReconfirmAttempts: null),
            RegistrationItem(secondId, "second@example.com", now.AddDays(-10), effectiveMaxReconfirmAttempts: null)
        ]);

        var job = BuildJob(facade, new FakeTimeProvider(now));
        await job.Execute(JobContext(eventId, minEmailIntervalHours: 0));

        var bulkJobs = await LoadBulkEmailJobsAsync();
        bulkJobs.Count.ShouldBe(1);
        bulkJobs[0].AttendeeFilter.RegistrationIds.ShouldBe([firstId, secondId], ignoreOrder: true);
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

    // Given an attendee who has received fewer reconfirm emails than their maximum allowed attempts
    // When the reconfirmations job runs
    // Then the attendee is included in the bulk reconfirm job and no auto-cancel event is published
    [TestMethod]
    public async ValueTask Execute_AllCandidatesBelowThreshold_DoesNotPublishAutoCancelEvent()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var attendeeId = Guid.NewGuid();

        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", now.AddDays(-3))));

        var facade = FacadeReturning(eventId, [
            RegistrationItem(attendeeId, "alice@example.com", now.AddDays(-10), effectiveMaxReconfirmAttempts: 3)
        ]);

        var job = BuildJob(facade, new FakeTimeProvider(now));
        await job.Execute(JobContext(eventId, minEmailIntervalHours: 0));

        var bulkJobs = await LoadBulkEmailJobsAsync();
        bulkJobs.Count.ShouldBe(1);
        var filter = bulkJobs[0].AttendeeFilter;
        filter.RegistrationIds.ShouldNotBeNull();
        filter.RegistrationIds.ShouldContain(attendeeId);
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

    private static RegistrationListItemDto RegistrationItem(
        Guid registrationId,
        string email,
        DateTimeOffset createdAt,
        int? effectiveMaxReconfirmAttempts = null) =>
        new(
            RegistrationId: registrationId,
            Email: email,
            FirstName: "Alice",
            LastName: "Test",
            TicketTypeIds: [],
            AdditionalDetails: new Dictionary<string, string>(),
            CreatedAt: createdAt,
            Status: RegistrationStatus.Registered,
            HasReconfirmed: false,
            ReconfirmedAt: null,
            EffectiveMaxReconfirmAttempts: effectiveMaxReconfirmAttempts);

    private static IRegistrationsFacade FacadeReturning(TicketedEventId eventId, IReadOnlyList<RegistrationListItemDto> candidates)
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade
            .GetRegistrationsAsync(
                TeamId.Value,
                eventId.Value,
                Arg.Is<QueryRegistrationsDto>(q =>
                    q != null &&
                    q.RegistrationStatus == RegistrationStatus.Registered &&
                    q.HasReconfirmed == false &&
                    q.RegistrationIds == null),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(candidates));
        return facade;
    }

    private static EmailLog ReconfirmEmailLog(TicketedEventId eventId, string recipientEmail, DateTimeOffset sentAt) =>
        EmailLog.Create(
            teamId: TeamId,
            ticketedEventId: eventId,
            idempotencyKey: $"reconfirm:{Guid.NewGuid():N}",
            recipient: EmailAddress.From(recipientEmail),
            emailType: BuiltInEmailTemplateNames.Reconfirmation,
            subject: "Please reconfirm",
            status: EmailLogStatus.Sent,
            sentAt: sentAt,
            statusUpdatedAt: sentAt);

    private static RequestReconfirmationsJob BuildJob(IRegistrationsFacade facade, TimeProvider timeProvider)
    {
        var ctx = Environment.EmailDatabase.Context;
        IEmailWriteStore writeStore = ctx;
        var outbox = new Outbox(ctx);
        IUnitOfWork unitOfWork = new UnitOfWork<EmailDbContext>(ctx, new NoOpOutboxMessageSender(), NullLogger<UnitOfWork<EmailDbContext>>.Instance);

        return new RequestReconfirmationsJob(
            writeStore,
            facade,
            outbox,
            unitOfWork,
            timeProvider,
            NullLogger<RequestReconfirmationsJob>.Instance);
    }

    private static IJobExecutionContext JobContext(TicketedEventId eventId, int minEmailIntervalHours)
    {
        var data = new JobDataMap
        {
            [RequestReconfirmationsJob.TeamIdKey] = TeamId.Value.ToString(),
            [RequestReconfirmationsJob.TicketedEventIdKey] = eventId.Value.ToString(),
            [RequestReconfirmationsJob.MinEmailIntervalHoursKey] = minEmailIntervalHours.ToString(),
        };

        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(data);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private static async Task<List<BulkEmailJob>> LoadBulkEmailJobsAsync()
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await Environment.EmailDatabase.Context.BulkEmailJobs.AsNoTracking().ToListAsync();
    }

    private static async Task<List<OutboxMessage>> LoadOutboxMessagesAsync()
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await Environment.EmailDatabase.Context.OutboxMessages.AsNoTracking().ToListAsync();
    }

    private static IReadOnlyList<Guid> GetRegistrationIds(JsonDocument payload) =>
        payload.RootElement.GetProperty("registrationIds")
            .EnumerateArray()
            .Select(x => x.GetGuid())
            .ToList();
}
