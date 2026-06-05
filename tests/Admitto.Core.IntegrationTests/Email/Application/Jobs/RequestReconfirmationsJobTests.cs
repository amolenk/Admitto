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
        var source = jobs[0].Source.ShouldBeOfType<AttendeeSource>();
        source.Filter.RegistrationIds.ShouldNotBeNull();
        source.Filter.RegistrationIds.ShouldContain(attendeeId);
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

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
        var source = bulkJobs[0].Source.ShouldBeOfType<AttendeeSource>();
        source.Filter.RegistrationIds.ShouldBe([sessionId], ignoreOrder: true);

        var outboxMessages = await LoadOutboxMessagesAsync();
        outboxMessages.Count.ShouldBe(1);
        outboxMessages[0].Type.ShouldBe($"{nameof(Email)}:{nameof(ReconfirmAutoExpiredIntegrationEvent)}");
        GetRegistrationIds(outboxMessages[0].Payload).ShouldBe([workshopId], ignoreOrder: true);
    }

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
        bulkJobs[0].Source.ShouldBeOfType<AttendeeSource>().Filter.RegistrationIds.ShouldBe([firstId, secondId], ignoreOrder: true);
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

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
        var source = bulkJobs[0].Source.ShouldBeOfType<AttendeeSource>();
        source.Filter.RegistrationIds.ShouldNotBeNull();
        source.Filter.RegistrationIds.ShouldContain(attendeeId);
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
                eventId.Value,
                Arg.Is<QueryRegistrationsDto>(q =>
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
            provider: "Test",
            providerMessageId: null,
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
