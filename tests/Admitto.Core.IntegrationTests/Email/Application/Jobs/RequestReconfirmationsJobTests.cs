using System.Text.Json;
using Amolenk.Admitto.Core.Email;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs.Fakes;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Application;
using Amolenk.Admitto.Testing.Builders.Email.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

[TestClass]
public sealed class RequestReconfirmationsJobTests : AspireIntegrationTestBase
{
    private static readonly TeamId TeamId = TeamId.New();

    // Given an active policy and an attendee who registered too recently
    // When the stable hourly evaluator runs
    // Then no reconfirm email job is created
    [TestMethod]
    public async ValueTask Execute_AttendeeRegisteredRecently_ExcludedFromBulkJob()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now);
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddHours(-10))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
    }

    // Given an active policy and a sent reconfirm email inside the minimum interval
    // When the stable hourly evaluator runs
    // Then the attendee is not included in a new reconfirm email job
    [TestMethod]
    public async ValueTask Execute_AttendeeReceivedReconfirmRecently_ExcludedFromBulkJob()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var attendeeId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, attendeeId, "alice@example.com", now.AddHours(-10),
                registrationCycleId: cycleId)));
        var facade = FacadeReturning(eventId, [RegistrationItem(attendeeId, "alice@example.com", now.AddHours(-72), cycleId: cycleId)]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
    }

    // Given an active policy and an attendee whose interval has elapsed
    // When the stable hourly evaluator runs
    // Then one reconfirm email job is created for that attendee
    [TestMethod]
    public async ValueTask Execute_MinEmailIntervalElapsedSinceLastEmail_AttendeeIncluded()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var attendeeId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(eventId, attendeeId, "alice@example.com", now.AddHours(-72),
                registrationCycleId: cycleId)));
        var facade = FacadeReturning(eventId, [RegistrationItem(attendeeId, "alice@example.com", now.AddHours(-100), cycleId: cycleId)]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        var jobs = await LoadBulkEmailJobsAsync();
        jobs.Count.ShouldBe(1);
        jobs[0].AttendeeFilter.RegistrationIds!.ShouldContain(attendeeId);
    }

    // Given an active reconfirm job already reserved for an event
    // When the hourly evaluator runs again
    // Then it does not create a second overlapping reconfirm job
    [TestMethod]
    public async ValueTask Execute_ActiveReconfirmJobExists_SkipsReservation()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db => db.BulkEmailJobs.Add(
            BulkEmailJob.CreateSystemTriggered(
                TeamId,
                eventId,
                BuiltInEmailTemplateNames.Reconfirmation,
                null,
                null,
                null,
                new BulkEmailAttendeeFilter(
                    RegistrationStatus: RegistrationStatus.Registered,
                    HasReconfirmed: false),
                now)));
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).Count.ShouldBe(1);
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given archived, cleared, partial, future, and closed projected policies
    // When the hourly evaluator runs
    // Then only the closed policy receives its terminal evaluation
    [TestMethod]
    public async ValueTask Execute_NonEligibleProjectedPolicies_EvaluatesOnlyClosedPolicy()
    {
        var now = DateTimeOffset.UtcNow;
        var archived = TicketedEventId.New();
        var cleared = TicketedEventId.New();
        var partial = TicketedEventId.New();
        var future = TicketedEventId.New();
        var closed = TicketedEventId.New();
        await SeedPolicyAsync(archived, now, archived: true);
        await SeedPolicyAsync(cleared, now, withoutPolicy: true);
        await SeedPolicyAsync(partial, now, withoutEventContext: true);
        await SeedPolicyAsync(future, now, opensAt: now.AddHours(1), closesAt: now.AddHours(2));
        await SeedPolicyAsync(closed, now, opensAt: now.AddHours(-2), closesAt: now);
        var facade = Substitute.For<IRegistrationsFacade>();

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        await facade.Received(1).GetRegistrationsAsync(
            TeamId.Value,
            closed.Value,
            Arg.Any<QueryRegistrationsDto>(),
            Arg.Any<CancellationToken>());
    }

    // Given one policy opening now and another closing now
    // When the hourly evaluator runs at the boundary
    // Then the open policy creates a reminder and the closing policy creates none
    [TestMethod]
    public async ValueTask Execute_WindowBoundaries_UsesOpenInclusiveCloseExclusive()
    {
        var now = DateTimeOffset.UtcNow;
        var opensNow = TicketedEventId.New();
        var closesNow = TicketedEventId.New();
        await SeedPolicyAsync(opensNow, now, opensAt: now, closesAt: now.AddHours(1));
        await SeedPolicyAsync(closesNow, now, opensAt: now.AddHours(-1), closesAt: now);
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<QueryRegistrationsDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<RegistrationListItemDto>>([
                RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]));

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).Count.ShouldBe(1);
        await facade.Received(1).GetRegistrationsAsync(
            TeamId.Value,
            opensNow.Value,
            Arg.Any<QueryRegistrationsDto>(),
            Arg.Any<CancellationToken>());
    }

    // Given a non-hour policy close and an unrelated open policy
    // When the one-shot close trigger runs
    // Then only the targeted policy receives terminal evaluation
    [TestMethod]
    public async ValueTask Execute_PolicyCloseTrigger_TargetsOnlyItsPolicy()
    {
        var now = new DateTimeOffset(2030, 6, 1, 12, 17, 31, TimeSpan.Zero);
        var targetedEventId = TicketedEventId.New();
        var unrelatedEventId = TicketedEventId.New();
        await SeedPolicyAsync(targetedEventId, now, closesAt: now);
        await SeedPolicyAsync(
            unrelatedEventId,
            now,
            opensAt: now.AddHours(-1),
            closesAt: now.AddHours(1));
        var facade = FacadeReturning(
            targetedEventId,
            [RegistrationItem(Guid.NewGuid(), "targeted@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(
            PolicyCloseJobContext(targetedEventId, now));

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        await facade.Received(1).GetRegistrationsAsync(
            TeamId.Value,
            targetedEventId.Value,
            Arg.Any<QueryRegistrationsDto>(),
            Arg.Any<CancellationToken>());
        await facade.DidNotReceive().GetRegistrationsAsync(
            TeamId.Value,
            unrelatedEventId.Value,
            Arg.Any<QueryRegistrationsDto>(),
            Arg.Any<CancellationToken>());
    }

    // Given an active policy with an invalid projected timezone
    // When the hourly evaluator runs
    // Then the event is skipped rather than evaluated outside quiet hours
    [TestMethod]
    public async ValueTask Execute_InvalidProjectedTimezone_SkipsEvent()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now, timeZone: "Not/AReal_Zone");
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given a maxed-out attendee and sent reconfirmation emails before the interval
    // When the hourly evaluator runs
    // Then it publishes automatic expiry without creating another email job
    [TestMethod]
    public async ValueTask Execute_MaxReconfirmationEmailsReached_PublishesAutoExpiry()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var registrationId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(ReconfirmEmailLog(eventId, registrationId, "alice@example.com", now.AddDays(-4),
                registrationCycleId: cycleId));
            db.EmailLog.Add(ReconfirmEmailLog(eventId, registrationId, "alice@example.com", now.AddDays(-3),
                registrationCycleId: cycleId));
        });
        var facade = FacadeReturning(eventId, [RegistrationItem(
            registrationId, "alice@example.com", now.AddDays(-10), effectiveMaxReconfirmationEmails: 2,
            cycleId: cycleId)]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        var outbox = await LoadOutboxMessagesAsync();
        outbox.Count.ShouldBe(1);
        GetRegistrationIds(outbox[0].Payload).ShouldBe([registrationId], ignoreOrder: true);
        outbox[0].Payload.RootElement.GetProperty("registrationReferences")[0]
            .GetProperty("registrationCycleId").GetGuid().ShouldBe(cycleId);
    }

    // Given an attendee with a delivered reconfirmation email at the maximum
    // When the hourly evaluator runs
    // Then it publishes automatic expiry without creating another email job
    [TestMethod]
    public async ValueTask Execute_DeliveredReconfirmationEmailReachesMaximum_PublishesAutoExpiry()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var registrationId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(
                eventId,
                registrationId,
                "alice@example.com",
                now.AddDays(-1),
                registrationCycleId: cycleId,
                status: EmailLogStatus.Delivered)));
        var facade = FacadeReturning(eventId, [RegistrationItem(
            registrationId,
            "alice@example.com",
            now.AddDays(-2),
            effectiveMaxReconfirmationEmails: 1,
            cycleId: cycleId)]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        (await LoadOutboxMessagesAsync()).ShouldHaveSingleItem();
    }

    // Given an active policy closing now with maxed and below-max attendees during quiet hours
    // When the hourly evaluator reaches the exclusive close boundary
    // Then it cancels only the maxed attendee without creating a reminder job
    [TestMethod]
    public async ValueTask Execute_AtPolicyClose_CancelsOnlyMaxedAttendeesWithoutReminder()
    {
        var now = new DateTimeOffset(2030, 6, 1, 23, 0, 0, TimeSpan.Zero);
        var fixture = ReconfirmPolicyCloseFixture.MaxedAndBelowMaximum(now);
        await fixture.SetupAsync(Environment);

        await BuildJob(fixture.Facade(), new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        var outbox = await LoadOutboxMessagesAsync();
        outbox.ShouldHaveSingleItem();
        GetRegistrationIds(outbox[0].Payload).ShouldBe([fixture.MaxedRegistrationId]);
    }

    // Given an active policy that has already reached its close boundary
    // When the hourly evaluator runs again for the same policy close
    // Then it does not publish a second automatic expiry event
    [TestMethod]
    public async ValueTask Execute_AtPolicyClose_RedeliveryIsIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var fixture = ReconfirmPolicyCloseFixture.SingleMaxed(now);
        await fixture.SetupAsync(Environment);
        var job = BuildJob(fixture.Facade(), new FakeTimeProvider(now));

        await job.Execute(JobContext());
        await job.Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        (await LoadOutboxMessagesAsync()).ShouldHaveSingleItem();
    }

    // Given a registered attendee with only failed and pending reconfirmation logs
    // When the hourly evaluator runs
    // Then the attendee remains eligible because only sent emails count
    [TestMethod]
    public async ValueTask Execute_OnlyUnsentReconfirmationLogs_StillCreatesEmailJob()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var registrationId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(UnsentReconfirmEmailLog(
                eventId, registrationId, "alice@example.com", EmailLogStatus.Failed, now.AddDays(-2)));
            db.EmailLog.Add(UnsentReconfirmEmailLog(
                eventId, registrationId, "alice@example.com", EmailLogStatus.Pending, now.AddDays(-1)));
        });
        var facade = FacadeReturning(eventId, [RegistrationItem(
            registrationId, "alice@example.com", now.AddDays(-10), effectiveMaxReconfirmationEmails: 1,
            cycleId: Guid.NewGuid())]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        var jobs = await LoadBulkEmailJobsAsync();
        var job = jobs.ShouldHaveSingleItem();
        job.AttendeeFilter.RegistrationIds.ShouldNotBeNull();
        job.AttendeeFilter.RegistrationIds.ShouldContain(registrationId);
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

    // Given a registration reset after a previous cycle had sent reconfirmation emails
    // When the hourly evaluator runs for the fresh cycle
    // Then prior-cycle emails do not exhaust the maximum
    [TestMethod]
    public async ValueTask Execute_ReconfirmationLogsBeforeRegistrationCycle_AreIgnored()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var registrationId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.EmailLog.Add(ReconfirmEmailLog(
                eventId, registrationId, "alice@example.com", now.AddDays(-10)));
        });
        var facade = FacadeReturning(eventId, [RegistrationItem(
            registrationId, "alice@example.com", now.AddDays(-2), effectiveMaxReconfirmationEmails: 1,
            cycleId: Guid.NewGuid())]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        var jobs = await LoadBulkEmailJobsAsync();
        var job = jobs.ShouldHaveSingleItem();
        job.AttendeeFilter.RegistrationIds.ShouldNotBeNull();
        job.AttendeeFilter.RegistrationIds.ShouldContain(registrationId);
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

    // Given a sent reconfirmation email from a different cycle inside the fresh cycle's dates
    // When the hourly evaluator runs
    // Then the mismatched-cycle email does not exhaust the current maximum
    [TestMethod]
    public async ValueTask Execute_ReconfirmationLogFromDifferentCycle_IsIgnored()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var registrationId = Guid.NewGuid();
        var currentCycleId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(
                eventId,
                registrationId,
                "alice@example.com",
                now.AddDays(-1),
                registrationCycleId: Guid.NewGuid())));
        var facade = FacadeReturning(eventId, [RegistrationItem(
            registrationId,
            "alice@example.com",
            now.AddDays(-2),
            effectiveMaxReconfirmationEmails: 1,
            cycleId: currentCycleId)]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        var jobs = await LoadBulkEmailJobsAsync();
        var job = jobs.ShouldHaveSingleItem();
        job.AttendeeFilter.RegistrationIds.ShouldNotBeNull();
        job.AttendeeFilter.RegistrationIds.ShouldContain(registrationId);
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

    // Given a fresh registration with a legacy null-cycle reconfirmation log
    // When the hourly evaluator runs
    // Then the unknown-cycle log does not exhaust the explicit current cycle
    [TestMethod]
    public async ValueTask Execute_NullCycleReconfirmationLog_IsIgnoredForExplicitCycle()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var registrationId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        await SeedPolicyAsync(eventId, now);
        await Environment.EmailDatabase.SeedAsync(db =>
            db.EmailLog.Add(ReconfirmEmailLog(
                eventId,
                registrationId,
                "alice@example.com",
                now.AddHours(-2),
                status: EmailLogStatus.Sent)));
        var facade = FacadeReturning(eventId, [RegistrationItem(
            registrationId,
            "alice@example.com",
            now.AddDays(-2),
            effectiveMaxReconfirmationEmails: 1,
            cycleId: cycleId)]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        var jobs = await LoadBulkEmailJobsAsync();
        jobs.ShouldHaveSingleItem();
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

    // Given an active policy with overnight quiet hours
    // When the hourly evaluation runs during the local quiet interval
    // Then no reconfirm email job is created
    [TestMethod]
    public async ValueTask Execute_DuringOvernightQuietHours_SkipsEvent()
    {
        var eventId = TicketedEventId.New();
        var now = new DateTimeOffset(2030, 6, 1, 23, 0, 0, TimeSpan.Zero);
        await SeedPolicyAsync(eventId, now, new TimeOnly(22, 0), new TimeOnly(8, 0));
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).ShouldBeEmpty();
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given a queued reminder drained during quiet hours and a live attendee state for the next hour
    // When the next permitted hourly evaluation runs
    // Then it creates a fresh job from the current facade result instead of resuming the drained snapshot
    [TestMethod]
    public async ValueTask Execute_QuietHourDrainedJob_NextPermittedEvaluationCreatesFreshJob()
    {
        var quietNow = new DateTimeOffset(2030, 6, 1, 23, 0, 0, TimeSpan.Zero);
        var nextPermittedHour = new DateTimeOffset(2030, 6, 2, 9, 0, 0, TimeSpan.Zero);
        var eventId = TicketedEventId.New();
        var registrationId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        await SeedPolicyAsync(
            eventId,
            quietNow,
            quietStart: new TimeOnly(22, 0),
            quietEnd: new TimeOnly(8, 0),
            opensAt: quietNow.AddDays(-1),
            closesAt: quietNow.AddDays(2));

        var recipient = BulkEmailJobBuilder.Recipient(
            "alice@example.com", "Alice", RegistrationCycleId.From(cycleId));
        var queuedJob = BulkEmailJob.CreateSystemTriggered(
            TeamId,
            eventId,
            BuiltInEmailTemplateNames.Reconfirmation,
            null,
            null,
            null,
            new BulkEmailAttendeeFilter(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false,
                RegistrationIds: [registrationId],
                RegistrationCycleIds: new Dictionary<Guid, Guid> { [registrationId] = cycleId }),
            quietNow);
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.BulkEmailJobs.Add(queuedJob);
            db.TeamEmailContexts.Add(SendBulkEmailJobFixture.CreateTeamEmailContext(TeamId));
        });

        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ReconfirmDeliveryState.Suppressed(ReconfirmDeliverySuppression.QuietHours));
        facade.GetRegistrationsAsync(
                TeamId.Value,
                eventId.Value,
                Arg.Is<QueryRegistrationsDto>(q => MatchesReconfirmQuery(q)),
                Arg.Any<CancellationToken>())
            .Returns([RegistrationItem(
                registrationId,
                "alice@example.com",
                quietNow.AddDays(-2),
                cycleId: cycleId)]);

        var resolver = Substitute.For<IBulkEmailRecipientResolver>();
        resolver.ResolveAsync(
                TeamId,
                eventId,
                Arg.Any<BulkEmailAttendeeFilter>(),
                Arg.Any<CancellationToken>())
            .Returns([recipient]);
        var sender = new FakeBulkSmtpSender();
        var fanOut = SendBulkEmailJobFixture.BuildExistingJobFanOutAt(
            Environment,
            sender,
            resolver,
            facade,
            new FakeTimeProvider(quietNow));

        await fanOut.Execute(SendBulkEmailJobFixture.JobContext(queuedJob));

        (await LoadBulkEmailJobsAsync()).Single().Status.ShouldBe(BulkEmailJobStatus.Completed);

        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ReconfirmDeliveryState.Allowed(
                quietNow.AddDays(-2), TimeSpan.FromHours(1), null, nextPermittedHour.AddHours(1)));
        await BuildJob(facade, new FakeTimeProvider(nextPermittedHour)).Execute(JobContext());

        var jobs = await LoadBulkEmailJobsAsync();
        jobs.Count.ShouldBe(2);
        var freshJob = jobs.Single(j => j.Id != queuedJob.Id);
        freshJob.Status.ShouldBe(BulkEmailJobStatus.Pending);
        freshJob.AttendeeFilter.RegistrationIds.ShouldBe([registrationId]);
        freshJob.AttendeeFilter.RegistrationCycleIds![registrationId].ShouldBe(cycleId);
    }

    // Given active policies for two events
    // When the single hourly evaluation runs
    // Then each eligible event is committed independently
    [TestMethod]
    public async ValueTask Execute_MultipleActivePolicies_CreatesOneJobPerEligibleEvent()
    {
        var firstEvent = TicketedEventId.New();
        var secondEvent = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(firstEvent, now);
        await SeedPolicyAsync(secondEvent, now);
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<QueryRegistrationsDto>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult<IReadOnlyList<RegistrationListItemDto>>([
                RegistrationItem(Guid.NewGuid(), $"{call.ArgAt<Guid>(1)}@example.com", now.AddDays(-2))]));

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBulkEmailJobsAsync()).Count.ShouldBe(2);
    }

    private async Task SeedPolicyAsync(
        TicketedEventId eventId,
        DateTimeOffset now,
        TimeOnly? quietStart = null,
        TimeOnly? quietEnd = null,
        DateTimeOffset? opensAt = null,
        DateTimeOffset? closesAt = null,
        bool archived = false,
        bool withoutPolicy = false,
        bool withoutEventContext = false,
        string timeZone = "UTC")
    {
        var builder = new EventEmailContextViewBuilder()
            .ForTeam(TeamId)
            .ForEvent(eventId)
            .At(now)
            .WithTimeZone(timeZone)
            .WithWindow(opensAt ?? now.AddHours(-1), closesAt ?? now.AddHours(1));
        if (quietStart.HasValue && quietEnd.HasValue)
            builder.WithQuietHours(quietStart.Value, quietEnd.Value);
        if (archived)
            builder.Archived();
        if (withoutPolicy)
            builder.WithoutReconfirmPolicy();
        if (withoutEventContext)
            builder.WithoutEventContext();

        await Environment.EmailDatabase.SeedAsync(db => db.EventEmailContexts.Add(builder.Build()));
    }

    private static RegistrationListItemDto RegistrationItem(
        Guid registrationId,
        string email,
        DateTimeOffset createdAt,
        int? effectiveMaxReconfirmationEmails = null,
        Guid? cycleId = null) =>
        new(registrationId, email, "Alice", "Test", [], new Dictionary<string, string>(), createdAt,
            cycleId ?? Guid.NewGuid(), 1, 1, RegistrationStatus.Registered, false, null,
            effectiveMaxReconfirmationEmails);

    private static IRegistrationsFacade FacadeReturning(
        TicketedEventId eventId,
        IReadOnlyList<RegistrationListItemDto> candidates)
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                TeamId.Value,
                eventId.Value,
                Arg.Is<QueryRegistrationsDto>(q => MatchesReconfirmQuery(q)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(candidates));
        return facade;
    }

    private static bool MatchesReconfirmQuery(QueryRegistrationsDto? query) =>
        query is not null
        && query.RegistrationStatus == RegistrationStatus.Registered
        && query.HasReconfirmed == false;

    private static EmailLog ReconfirmEmailLog(
        TicketedEventId eventId,
        Guid? registrationId,
        string email,
        DateTimeOffset sentAt,
        Guid? registrationCycleId = null,
        EmailLogStatus status = EmailLogStatus.Sent) =>
        EmailLog.Create(TeamId, eventId, $"reconfirm:{Guid.NewGuid():N}", EmailAddress.From(email),
            BuiltInEmailTemplateNames.Reconfirmation, "Please reconfirm", status, sentAt, sentAt,
            registrationId: registrationId is null ? null : RegistrationId.From(registrationId.Value),
            registrationCycleId: registrationCycleId is null ? null : RegistrationCycleId.From(registrationCycleId.Value));

    private static EmailLog UnsentReconfirmEmailLog(
        TicketedEventId eventId,
        Guid registrationId,
        string email,
        EmailLogStatus status,
        DateTimeOffset statusUpdatedAt) =>
        EmailLog.Create(TeamId, eventId, $"reconfirm:{Guid.NewGuid():N}", EmailAddress.From(email),
            BuiltInEmailTemplateNames.Reconfirmation, "Please reconfirm", status, null, statusUpdatedAt,
            registrationId: RegistrationId.From(registrationId));

    private RequestReconfirmationsJob BuildJob(
        IRegistrationsFacade facade,
        TimeProvider timeProvider)
    {
        var ctx = Environment.EmailDatabase.Context;
        var services = new ServiceCollection()
            .AddScoped<IEmailWriteStore>(_ => new TestEmailWriteStore(ctx))
            .AddScoped<IRegistrationsFacade>(_ => facade)
            .AddKeyedScoped<IOutbox>(EmailModule.Key, (_, _) => new Outbox(ctx))
            .AddKeyedScoped<IUnitOfWork>(EmailModule.Key, (_, _) => new UnitOfWork<EmailDbContext>(
                ctx, new NoOpOutboxMessageSender(), NullLogger<UnitOfWork<EmailDbContext>>.Instance))
            .BuildServiceProvider();

        return new RequestReconfirmationsJob(
            ctx,
            services.GetRequiredService<IServiceScopeFactory>(),
            timeProvider,
            NullLogger<RequestReconfirmationsJob>.Instance);
    }

    private static IJobExecutionContext JobContext()
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    private static IJobExecutionContext PolicyCloseJobContext(
        TicketedEventId eventId,
        DateTimeOffset closesAt)
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(CancellationToken.None);
        context.MergedJobDataMap.Returns(new JobDataMap
        {
            [RequestReconfirmationsJob.PolicyCloseEventIdKey] = eventId.Value.ToString(),
            [RequestReconfirmationsJob.PolicyCloseAtKey] =
                closesAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        });
        return context;
    }

    private sealed class TestEmailWriteStore(EmailDbContext context) : IEmailWriteStore
    {
        public DbSet<EmailLog> EmailLog => context.EmailLog;
        public DbSet<BulkEmailJob> BulkEmailJobs => context.BulkEmailJobs;
        public DbSet<ReconfirmPolicyCloseEvaluation> ReconfirmPolicyCloseEvaluations =>
            context.ReconfirmPolicyCloseEvaluations;
    }

    private async Task<List<BulkEmailJob>> LoadBulkEmailJobsAsync()
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await Environment.EmailDatabase.Context.BulkEmailJobs.AsNoTracking().ToListAsync();
    }

    private async Task<List<OutboxMessage>> LoadOutboxMessagesAsync()
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
