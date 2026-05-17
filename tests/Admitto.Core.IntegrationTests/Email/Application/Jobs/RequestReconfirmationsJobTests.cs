using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs.Fakes;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

/// <summary>
/// Integration tests for <see cref="RequestReconfirmationsJob"/> verifying that
/// the MinEmailInterval throttle correctly excludes or includes attendees before
/// the <see cref="BulkEmailJob"/> is created.
/// </summary>
[TestClass]
public sealed class RequestReconfirmationsJobTests : AspireIntegrationTestBase
{
    private static readonly TeamId _teamId = TeamId.New();

    [TestMethod]
    public async ValueTask Execute_AttendeeRegisteredRecently_ExcludedFromBulkJob()
    {
        // Attendee registered < MinEmailInterval hours ago → no bulk job created.
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var minIntervalHours = 48;

        var candidates = new[]
        {
            // Registered only 10 hours ago — inside the throttle window.
            RegistrationItem(Guid.NewGuid(), "alice@example.com", createdAt: now.AddHours(-10)),
        };

        var facade = FacadeReturning(eventId, candidates);
        var job = BuildJob(facade, new FakeTimeProvider(now));

        await job.Execute(JobContext(eventId, minIntervalHours));

        // No eligible attendees → no BulkEmailJob should be created.
        var jobs = await LoadBulkEmailJobsAsync();
        jobs.ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask Execute_AttendeeReceivedReconfirmRecently_ExcludedFromBulkJob()
    {
        // Reconfirmation sent within MinEmailInterval → attendee excluded.
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var minIntervalHours = 48;

        var candidates = new[]
        {
            // Registered 72 hours ago (beyond the interval), but last reconfirm
            // was sent only 10 hours ago (within the interval).
            RegistrationItem(Guid.NewGuid(), "alice@example.com", createdAt: now.AddHours(-72)),
        };

        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", sentAt: now.AddHours(-10))));

        var facade = FacadeReturning(eventId, candidates);
        var job = BuildJob(facade, new FakeTimeProvider(now));

        await job.Execute(JobContext(eventId, minIntervalHours));

        // Throttled by last-sent time → no BulkEmailJob.
        var jobs = await LoadBulkEmailJobsAsync();
        jobs.ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask Execute_MinEmailIntervalElapsedSinceLastEmail_AttendeeIncluded()
    {
        // MinEmailInterval fully elapsed since last contact → attendee included.
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var minIntervalHours = 48;

        var attendeeId = Guid.NewGuid();
        var candidates = new[]
        {
            // Registered 100 hours ago; last reconfirm was sent 72 hours ago —
            // both beyond the 48 h threshold.
            RegistrationItem(attendeeId, "alice@example.com", createdAt: now.AddHours(-100)),
        };

        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, "alice@example.com", sentAt: now.AddHours(-72))));

        var facade = FacadeReturning(eventId, candidates);
        var job = BuildJob(facade, new FakeTimeProvider(now));

        await job.Execute(JobContext(eventId, minIntervalHours));

        // Eligible → one BulkEmailJob created, scoped to the attendee's registration ID.
        var jobs = await LoadBulkEmailJobsAsync();
        jobs.Count.ShouldBe(1);

        var source = jobs[0].Source as AttendeeSource;
        source.ShouldNotBeNull();
        source.Filter.RegistrationIds.ShouldNotBeNull();
        source.Filter.RegistrationIds.ShouldContain(attendeeId);
    }

    // --- helpers --------------------------------------------------------

    private static RegistrationListItemDto RegistrationItem(
        Guid registrationId,
        string email,
        DateTimeOffset createdAt) =>
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
            ReconfirmedAt: null);

    private static IRegistrationsFacade FacadeReturning(
        TicketedEventId eventId,
        IReadOnlyList<RegistrationListItemDto> candidates)
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade
            .QueryRegistrationsAsync(
                eventId,
                Arg.Is<QueryRegistrationsDto>(q =>
                    q.RegistrationStatus == RegistrationStatus.Registered &&
                    q.HasReconfirmed == false &&
                    q.RegistrationIds == null),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<RegistrationListItemDto>>(candidates));
        return facade;
    }

    private static EmailLog ReconfirmEmailLog(
        TicketedEventId eventId,
        string recipientEmail,
        DateTimeOffset sentAt) =>
        EmailLog.Create(
            teamId: _teamId,
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

    private static RequestReconfirmationsJob BuildJob(
        IRegistrationsFacade facade,
        TimeProvider timeProvider)
    {
        var ctx = Environment.EmailDatabase.Context;
        IEmailWriteStore writeStore = ctx;
        IUnitOfWork unitOfWork = new UnitOfWork<EmailDbContext>(ctx, new NoOpOutboxMessageSender(), NullLogger<UnitOfWork<EmailDbContext>>.Instance);

        return new RequestReconfirmationsJob(
            writeStore,
            facade,
            unitOfWork,
            timeProvider,
            NullLogger<RequestReconfirmationsJob>.Instance);
    }

    private static IJobExecutionContext JobContext(TicketedEventId eventId, int minEmailIntervalHours)
    {
        var data = new JobDataMap
        {
            [RequestReconfirmationsJob.TeamIdKey] = _teamId.Value.ToString(),
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
        return await Environment.EmailDatabase.Context.BulkEmailJobs
            .AsNoTracking().ToListAsync();
    }
}
