using System.Text.Json;
using Amolenk.Admitto.Core.Email;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
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
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

[TestClass]
public sealed class RequestReconfirmationsJobTests : AspireIntegrationTestBase
{
    private static readonly TeamId TeamId = TeamId.New();
    private FakeSmtpBatchSender _lastSender = default!;

    // Given an active policy and an attendee who registered too recently
    // When the stable hourly evaluator runs
    // Then no reconfirmation batch is created
    [TestMethod]
    public async ValueTask Execute_AttendeeRegisteredRecently_ExcludedFromBatch()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now);
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddHours(-10))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBatchesAsync()).ShouldBeEmpty();
    }

    // Given an active policy and a sent reconfirm email inside the minimum interval
    // When the stable hourly evaluator runs
    // Then the attendee is not included in a new reconfirmation batch
    [TestMethod]
    public async ValueTask Execute_AttendeeReceivedReconfirmRecently_ExcludedFromBatch()
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

        (await LoadBatchesAsync()).ShouldBeEmpty();
    }

    // Given an active policy and an attendee whose interval has elapsed
    // When the stable hourly evaluator runs
    // Then one reconfirmation batch is created for that attendee
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

        var jobs = await LoadBatchesAsync();
        jobs.Count.ShouldBe(1);
    }

    // Given an interrupted reconfirmation batch for an event
    // When the hourly evaluator runs again
    // Then it fails the old batch before creating fresh work
    [TestMethod]
    public async ValueTask Execute_ActiveReconfirmJobExists_SkipsReservation()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        await SeedPolicyAsync(eventId, now);
        var interrupted = ReconfirmationBatch.Create(TeamId, eventId, now.AddHours(-1));
        interrupted.BeginSending(now.AddHours(-1));
        await Environment.EmailDatabase.SeedAsync(db => db.ReconfirmationBatches.Add(interrupted));
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        var batches = await LoadBatchesAsync();
        batches.Count.ShouldBe(2);
        batches.Single(batch => batch.Id == interrupted.Id).Status.ShouldBe(ReconfirmationBatchStatus.Failed);
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

        (await LoadBatchesAsync()).ShouldBeEmpty();
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

        (await LoadBatchesAsync()).Count.ShouldBe(1);
        await facade.Received(1).GetRegistrationsAsync(
            TeamId.Value,
            opensNow.Value,
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

        (await LoadBatchesAsync()).ShouldBeEmpty();
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given a maxed-out attendee and sent reconfirmation emails before the interval
    // When the hourly evaluator runs
    // Then it publishes automatic expiry without creating another batch
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

        (await LoadBatchesAsync()).ShouldBeEmpty();
        var outbox = await LoadOutboxMessagesAsync();
        outbox.Count.ShouldBe(1);
        GetRegistrationIds(outbox[0].Payload).ShouldBe([registrationId], ignoreOrder: true);
        outbox[0].Payload.RootElement.GetProperty("registrationReferences")[0]
            .GetProperty("registrationCycleId").GetGuid().ShouldBe(cycleId);
    }

    // Given an attendee with a delivered reconfirmation email at the maximum
    // When the hourly evaluator runs
    // Then it publishes automatic expiry without creating another batch
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

        (await LoadBatchesAsync()).ShouldBeEmpty();
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

        (await LoadBatchesAsync()).ShouldBeEmpty();
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

        (await LoadBatchesAsync()).ShouldBeEmpty();
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

        var jobs = await LoadBatchesAsync();
        var job = jobs.ShouldHaveSingleItem();
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

        var jobs = await LoadBatchesAsync();
        var job = jobs.ShouldHaveSingleItem();
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

        var jobs = await LoadBatchesAsync();
        var job = jobs.ShouldHaveSingleItem();
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

        var jobs = await LoadBatchesAsync();
        jobs.ShouldHaveSingleItem();
        (await LoadOutboxMessagesAsync()).ShouldBeEmpty();
    }

    // Given an active policy with overnight quiet hours
    // When the hourly evaluation runs during the local quiet interval
    // Then no reconfirmation batch is created
    [TestMethod]
    public async ValueTask Execute_DuringOvernightQuietHours_SkipsEvent()
    {
        var eventId = TicketedEventId.New();
        var now = new DateTimeOffset(2030, 6, 1, 23, 0, 0, TimeSpan.Zero);
        await SeedPolicyAsync(eventId, now, new TimeOnly(22, 0), new TimeOnly(8, 0));
        var facade = FacadeReturning(eventId, [RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2))]);

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBatchesAsync()).ShouldBeEmpty();
        await facade.DidNotReceiveWithAnyArgs().GetRegistrationsAsync(default, default, default!, default);
    }

    // Given active policies for two events
    // When the single hourly evaluation runs
    // Then each eligible event is committed independently
    [TestMethod]
    public async ValueTask Execute_MultipleActivePolicies_CreatesOneBatchPerEligibleEvent()
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
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ReconfirmDeliveryState.Allowed(
                now.AddDays(-2), TimeSpan.FromHours(1), null, now.AddHours(1)));

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        (await LoadBatchesAsync()).Count.ShouldBe(2);
        _lastSender.SessionsOpened.ShouldBe(1);
        _lastSender.SessionsClosed.ShouldBe(1);
        _lastSender.SentMessages.Count.ShouldBe(2);
        (await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().CountAsync())
            .ShouldBe(2);
    }

    // Given two live candidates in one batch whose delivery crosses the requested deadline
    // When the hourly run sends the batch
    // Then both candidates finish through the shared SMTP session
    [TestMethod]
    public async ValueTask Execute_BatchCrossesRequestedDeadline_DeliversAllCandidates()
    {
        var start = new DateTimeOffset(2030, 6, 1, 21, 59, 0, TimeSpan.Zero);
        var close = start.AddMinutes(1);
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(
            eventId,
            start,
            opensAt: start.AddDays(-1),
            closesAt: close);
        var fakeTime = new FakeTimeProvider(start);
        var first = RegistrationItem(Guid.NewGuid(), "alice@example.com", start.AddDays(-2));
        var second = RegistrationItem(Guid.NewGuid(), "bob@example.com", start.AddDays(-2));
        var facade = FacadeReturning(eventId, [first, second]);
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<ReconfirmDeliveryQuery>()!;
                return query.Now >= close
                    ? new ReconfirmDeliveryState.Suppressed(ReconfirmDeliverySuppression.OutsideWindow)
                    : new ReconfirmDeliveryState.Allowed(
                        start.AddDays(-2), TimeSpan.FromHours(1), null, close);
            });

        var job = BuildJob(facade, fakeTime);
        _lastSender.OnBeforeSendAsync = message =>
        {
            if (message.RecipientAddress == "alice@example.com")
                fakeTime.Advance(TimeSpan.FromMinutes(2));
            return Task.CompletedTask;
        };

        await job.Execute(JobContext());

        var batches = await LoadBatchesAsync();
        batches.ShouldHaveSingleItem().Status.ShouldBe(ReconfirmationBatchStatus.Completed);
        _lastSender.SessionsOpened.ShouldBe(1);
        _lastSender.SentMessages.Count.ShouldBe(2);
    }

    // Given a later policy that reaches its requested deadline while an earlier policy sends
    // When the hourly job evaluates policies in sequence
    // Then the later policy performs terminal evaluation instead of starting a batch
    [TestMethod]
    public async ValueTask Execute_LaterPolicyCrossesDeadlineDuringEarlierDelivery_UsesFreshPolicyTime()
    {
        var start = new DateTimeOffset(2030, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var firstEvent = TicketedEventId.New();
        var laterEvent = TicketedEventId.New();
        await SeedPolicyAsync(
            firstEvent,
            start,
            opensAt: start.AddHours(-1),
            closesAt: start.AddHours(1));
        await SeedPolicyAsync(
            laterEvent,
            start.AddTicks(1),
            opensAt: start.AddHours(-1),
            closesAt: start.AddSeconds(30));

        var firstCandidate = RegistrationItem(Guid.NewGuid(), "first@example.com", start.AddDays(-2));
        var laterCandidate = RegistrationItem(Guid.NewGuid(), "later@example.com", start.AddDays(-2));
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetRegistrationsAsync(
                TeamId.Value,
                firstEvent.Value,
                Arg.Any<QueryRegistrationsDto>(),
                Arg.Any<CancellationToken>())
            .Returns([firstCandidate]);
        facade.GetRegistrationsAsync(
                TeamId.Value,
                laterEvent.Value,
                Arg.Any<QueryRegistrationsDto>(),
                Arg.Any<CancellationToken>())
            .Returns([laterCandidate]);
        facade.GetReconfirmDeliveryStateAsync(
                TeamId.Value,
                firstEvent.Value,
                Arg.Any<ReconfirmDeliveryQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(new ReconfirmDeliveryState.Allowed(
                firstCandidate.CreatedAt, TimeSpan.FromHours(1), null, start.AddHours(1)));

        var fakeTime = new FakeTimeProvider(start);
        var job = BuildJob(facade, fakeTime);
        _lastSender.OnBeforeSendAsync = message =>
        {
            if (message.RecipientAddress == firstCandidate.Email)
                fakeTime.Advance(TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        };

        await job.Execute(JobContext());

        var batches = await LoadBatchesAsync();
        batches.ShouldHaveSingleItem().TicketedEventId.ShouldBe(firstEvent);
        (await Environment.EmailDatabase.Context.ReconfirmPolicyCloseEvaluations.AsNoTracking()
                .CountAsync(e => e.TicketedEventId == laterEvent))
            .ShouldBe(1);
    }

    // Given candidate selection that advances into event quiet hours
    // When the hourly job reaches the batch reservation gate
    // Then it skips creating a batch
    [TestMethod]
    public async ValueTask Execute_CandidateSelectionEntersQuietHours_SkipsBatchCreation()
    {
        var start = new DateTimeOffset(2030, 6, 1, 21, 59, 0, TimeSpan.Zero);
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(
            eventId,
            start,
            quietStart: new TimeOnly(22, 0),
            quietEnd: new TimeOnly(23, 0),
            opensAt: start.AddHours(-1),
            closesAt: start.AddHours(1));
        var fakeTime = new FakeTimeProvider(start);
        var candidate = RegistrationItem(Guid.NewGuid(), "quiet@example.com", start.AddDays(-2));
        var facade = FacadeReturning(eventId, [candidate]);
        facade.GetRegistrationsAsync(
                TeamId.Value,
                eventId.Value,
                Arg.Any<QueryRegistrationsDto>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                fakeTime.Advance(TimeSpan.FromMinutes(2));
                return Task.FromResult<IReadOnlyList<RegistrationListItemDto>>([candidate]);
            });

        await BuildJob(facade, fakeTime).Execute(JobContext());

        (await LoadBatchesAsync()).ShouldBeEmpty();
    }

    // Given candidate selection that advances beyond the requested deadline
    // When the hourly job reaches the batch reservation gate
    // Then it performs terminal evaluation without creating a batch
    [TestMethod]
    public async ValueTask Execute_CandidateSelectionCrossesDeadline_PerformsTerminalEvaluation()
    {
        var start = new DateTimeOffset(2030, 6, 1, 21, 59, 0, TimeSpan.Zero);
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(
            eventId,
            start,
            opensAt: start.AddHours(-1),
            closesAt: start.AddMinutes(1));
        var fakeTime = new FakeTimeProvider(start);
        var candidate = RegistrationItem(Guid.NewGuid(), "deadline@example.com", start.AddDays(-2));
        var facade = FacadeReturning(eventId, [candidate]);
        facade.GetRegistrationsAsync(
                TeamId.Value,
                eventId.Value,
                Arg.Any<QueryRegistrationsDto>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                fakeTime.Advance(TimeSpan.FromMinutes(2));
                return Task.FromResult<IReadOnlyList<RegistrationListItemDto>>([candidate]);
            });

        await BuildJob(facade, fakeTime).Execute(JobContext());

        (await LoadBatchesAsync()).ShouldBeEmpty();
        (await Environment.EmailDatabase.Context.ReconfirmPolicyCloseEvaluations.AsNoTracking()
                .CountAsync(e => e.TicketedEventId == eventId))
            .ShouldBe(1);
    }

    // Given a live candidate whose state changes after its EmailLog claim
    // When admission is checked immediately before SMTP
    // Then the reminder is suppressed without a delivered message
    [TestMethod]
    public async ValueTask Execute_CandidateReconfirmedAfterClaim_SuppressesBeforeSmtp()
    {
        var now = DateTimeOffset.UtcNow;
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(eventId, now);
        var candidate = RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2));
        var facade = FacadeReturning(eventId, [candidate]);
        var admissionCalls = 0;
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++admissionCalls == 1
                ? new ReconfirmDeliveryState.Allowed(candidate.CreatedAt, TimeSpan.FromHours(1), null, now.AddHours(1))
                : new ReconfirmDeliveryState.Suppressed(ReconfirmDeliverySuppression.RegistrationReconfirmed));

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        _lastSender.SentMessages.ShouldBeEmpty();
        (await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().ToListAsync()).ShouldBeEmpty();
        (await LoadBatchesAsync()).ShouldHaveSingleItem().Status.ShouldBe(ReconfirmationBatchStatus.Completed);
    }

    // Given a candidate whose authoritative state changes after its EmailLog claim
    // When admission is checked before each SMTP attempt
    // Then cancelled, cycled, reticketed, archived, or disabled candidates are suppressed
    [TestMethod]
    [DataRow(nameof(ReconfirmDeliverySuppression.RegistrationCancelled))]
    [DataRow(nameof(ReconfirmDeliverySuppression.RegistrationCycleChanged))]
    [DataRow(nameof(ReconfirmDeliverySuppression.TicketSelectionChanged))]
    [DataRow(nameof(ReconfirmDeliverySuppression.EventNotActive))]
    [DataRow(nameof(ReconfirmDeliverySuppression.PolicyDisabled))]
    public async ValueTask Execute_AuthoritativeStateChangesAfterClaim_SuppressesBeforeSmtp(string reason)
    {
        var now = DateTimeOffset.UtcNow;
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(eventId, now);
        var candidate = RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2));
        var facade = FacadeReturning(eventId, [candidate]);
        var admissionCalls = 0;
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++admissionCalls == 1
                ? new ReconfirmDeliveryState.Allowed(candidate.CreatedAt, TimeSpan.FromHours(1), null, now.AddHours(1))
                : new ReconfirmDeliveryState.Suppressed(
                    Enum.Parse<ReconfirmDeliverySuppression>(reason)));

        await BuildJob(facade, new FakeTimeProvider(now)).Execute(JobContext());

        _lastSender.SentMessages.ShouldBeEmpty();
        (await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().ToListAsync()).ShouldBeEmpty();
    }

    // Given two candidates and an SMTP failure for one of them
    // When the hourly batch is delivered
    // Then successful and failed EmailLog outcomes are both recorded
    [TestMethod]
    public async ValueTask Execute_MixedSmtpResults_CompletesAndAuditsEachAttempt()
    {
        var now = DateTimeOffset.UtcNow;
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(eventId, now);
        var candidates = new[]
        {
            RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2)),
            RegistrationItem(Guid.NewGuid(), "bob@example.com", now.AddDays(-2))
        };
        var facade = FacadeReturning(eventId, candidates);
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ReconfirmDeliveryState.Allowed(now.AddDays(-2), TimeSpan.FromHours(1), null, now.AddHours(1)));

        var job = BuildJob(facade, new FakeTimeProvider(now));
        _lastSender.FailOn("bob@example.com");
        await job.Execute(JobContext());

        var logs = await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().ToListAsync();
        logs.Count.ShouldBe(2);
        logs.ShouldAllBe(log => log.ReconfirmationBatchId.HasValue);
        logs.Count(log => log.Status == EmailLogStatus.Sent).ShouldBe(1);
        logs.Count(log => log.Status == EmailLogStatus.Failed).ShouldBe(1);
    }

    // Given a previous batch whose SMTP attempt failed
    // When a later hourly run evaluates the same live candidate
    // Then the failed attempt does not consume the successful-email allowance
    [TestMethod]
    public async ValueTask Execute_FailedAttempt_DoesNotBlockFutureBatch()
    {
        var firstNow = DateTimeOffset.UtcNow;
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(eventId, firstNow, closesAt: firstNow.AddDays(1));
        var candidate = RegistrationItem(Guid.NewGuid(), "alice@example.com", firstNow.AddDays(-2));
        var facade = FacadeReturning(eventId, [candidate]);
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ReconfirmDeliveryState.Allowed(candidate.CreatedAt, TimeSpan.FromHours(1), 1, firstNow.AddHours(1)));

        var firstJob = BuildJob(facade, new FakeTimeProvider(firstNow));
        _lastSender.FailOn("alice@example.com");
        await firstJob.Execute(JobContext());

        var secondNow = firstNow.AddHours(2);
        await BuildJob(facade, new FakeTimeProvider(secondNow)).Execute(JobContext());

        var batches = await LoadBatchesAsync();
        batches.Count.ShouldBe(2);
        batches.ShouldAllBe(batch => batch.Status == ReconfirmationBatchStatus.Completed);
        var logs = await Environment.EmailDatabase.Context.EmailLog.AsNoTracking().ToListAsync();
        logs.Count.ShouldBe(2);
        logs.Count(log => log.Status == EmailLogStatus.Failed).ShouldBe(1);
        logs.Count(log => log.Status == EmailLogStatus.Sent).ShouldBe(1);
    }

    // Given an active batch for an event with no usable projected policy
    // When the hourly job starts
    // Then it fails the batch before evaluating fresh candidates for other events
    [TestMethod]
    public async ValueTask Execute_ActiveBatchesForArchivedOrIncompletePolicies_AreFailedAtJobStart()
    {
        var now = DateTimeOffset.UtcNow;
        var archived = TicketedEventId.New();
        var cleared = TicketedEventId.New();
        var incomplete = TicketedEventId.New();
        var future = TicketedEventId.New();
        await SeedPolicyAsync(archived, now, archived: true);
        await SeedPolicyAsync(cleared, now, withoutPolicy: true);
        await SeedPolicyAsync(incomplete, now, withoutEventContext: true);
        await SeedPolicyAsync(future, now, opensAt: now.AddHours(1), closesAt: now.AddHours(2));

        var batches = new[] { archived, cleared, incomplete, future }
            .Select(eventId =>
            {
                var batch = ReconfirmationBatch.Create(TeamId, eventId, now.AddMinutes(-5));
                batch.BeginSending(now.AddMinutes(-5));
                return batch;
            })
            .ToList();
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            db.ReconfirmationBatches.AddRange(batches);
            db.EmailLog.Add(EmailLog.Create(
                TeamId,
                archived,
                $"reconfirm:{batches[0].Id.Value:N}:pending",
                EmailAddress.From("abandoned@example.com"),
                BuiltInEmailTemplateNames.Reconfirmation,
                "Reconfirm",
                EmailLogStatus.Pending,
                null,
                now.AddMinutes(-4),
                reconfirmationBatchId: batches[0].Id,
                registrationId: RegistrationId.New(),
                registrationCycleId: RegistrationCycleId.New()));
        });

        await BuildJob(Substitute.For<IRegistrationsFacade>(), new FakeTimeProvider(now)).Execute(JobContext());

        var reloaded = await LoadBatchesAsync();
        reloaded.ShouldAllBe(batch => batch.Status == ReconfirmationBatchStatus.Failed);
        var abandonedClaim = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(log => log.ReconfirmationBatchId == batches[0].Id);
        abandonedClaim.Status.ShouldBe(EmailLogStatus.Failed);
    }

    // Given a candidate whose batch preparation fails after the batch is started
    // When the hourly job encounters the preparation failure
    // Then a fresh context records the batch as Failed
    [TestMethod]
    public async ValueTask Execute_BatchPreparationFails_PersistsFailedInFreshContext()
    {
        var now = DateTimeOffset.UtcNow;
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(eventId, now);
        var candidate = RegistrationItem(Guid.NewGuid(), "alice@example.com", now.AddDays(-2));
        var facade = FacadeReturning(eventId, [candidate]);

        await BuildJob(facade, new FakeTimeProvider(now), failEventContext: true).Execute(JobContext());

        var failed = (await LoadBatchesAsync()).ShouldHaveSingleItem();
        failed.Status.ShouldBe(ReconfirmationBatchStatus.Failed);
        failed.LastError!.ShouldContain("Induced event-context failure");
    }

    // Given a started batch whose worker cancellation interrupts SMTP delivery
    // When the hourly job executes with the cancellation token
    // Then it persists the batch as Failed before propagating cancellation
    [TestMethod]
    public async ValueTask Execute_CancellationInterruptsStartedBatch_PersistsFailed()
    {
        var now = DateTimeOffset.UtcNow;
        var eventId = TicketedEventId.New();
        await SeedPolicyAsync(eventId, now);
        var candidate = RegistrationItem(Guid.NewGuid(), "cancelled-worker@example.com", now.AddDays(-2));
        var facade = FacadeReturning(eventId, [candidate]);
        facade.GetReconfirmDeliveryStateAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<ReconfirmDeliveryQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ReconfirmDeliveryState.Allowed(candidate.CreatedAt, TimeSpan.FromHours(1), null, now.AddHours(1)));
        using var cancellation = new CancellationTokenSource();
        var job = BuildJob(facade, new FakeTimeProvider(now));
        _lastSender.OnBeforeSendAsync = _ =>
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        };

        await Should.ThrowAsync<OperationCanceledException>(
            () => job.Execute(JobContext(cancellation.Token)));

        (await LoadBatchesAsync()).ShouldHaveSingleItem().Status.ShouldBe(ReconfirmationBatchStatus.Failed);
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
        TimeProvider timeProvider,
        bool failEventContext = false)
    {
        var ctx = Environment.EmailDatabase.Context;
        var services = new ServiceCollection()
            .AddScoped<IEmailWriteStore>(_ => new TestEmailWriteStore(ctx))
            .AddScoped<IRegistrationsFacade>(_ => facade)
            .AddScoped<IEffectiveEmailSettingsResolver>(_ => new EffectiveEmailSettingsResolver(
                Options.Create(new SystemEmailOptions
                {
                    SmtpHost = "smtp.example.com",
                    SmtpPort = 587,
                    FromAddress = "tickets@admitto.org",
                    AuthMode = "None"
                }),
                ctx))
            .AddScoped<IEmailTemplateService>(_ => new EmailTemplateService())
            .AddSingleton<IEmailRenderer, ScribanEmailRenderer>()
            .AddSingleton<ISmtpBatchSender>(_lastSender = new FakeSmtpBatchSender())
            .AddSingleton<IOptionsMonitor<EmailDeliveryOptions>>(
                new StaticOptionsMonitor<EmailDeliveryOptions>(new EmailDeliveryOptions
                {
                    PerMessageDelay = TimeSpan.Zero,
                    InlineRetryDelay = TimeSpan.Zero
                }))
            .AddScoped<IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>>(_ =>
                new TestEventEmailContextQuery(failEventContext))
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

    private sealed class TestEventEmailContextQuery(bool fail)
        : IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>
    {
        public ValueTask<EventEmailContextDto> HandleAsync(
            GetEventEmailRenderingContextQuery query,
            CancellationToken cancellationToken)
        {
            if (fail)
                throw new InvalidOperationException("Induced event-context failure.");

            return
            ValueTask.FromResult(new EventEmailContextDto(
                query.TeamId.Value,
                query.TicketedEventId.Value,
                "DevConf Team",
                "DevConf",
                "https://example.com",
                "https://tickets.example.com/e/devconf",
                "https://tickets.example.com/e/devconf/register",
                "https://tickets.example.com/e/devconf/qr-code",
                "https://tickets.example.com/e/devconf/cancel",
                "https://tickets.example.com/e/devconf/edit",
                "UTC",
                null,
                null,
                null,
                false));
        }
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private static IJobExecutionContext JobContext(CancellationToken cancellationToken = default)
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.CancellationToken.Returns(cancellationToken);
        return context;
    }

    private sealed class TestEmailWriteStore(EmailDbContext context) : IEmailWriteStore
    {
        public DbSet<EmailLog> EmailLog => context.EmailLog;
        public DbSet<ReconfirmationBatch> ReconfirmationBatches => context.ReconfirmationBatches;
        public DbSet<ReconfirmPolicyCloseEvaluation> ReconfirmPolicyCloseEvaluations =>
            context.ReconfirmPolicyCloseEvaluations;
    }

    private async Task<List<ReconfirmationBatch>> LoadBatchesAsync()
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await Environment.EmailDatabase.Context.ReconfirmationBatches.AsNoTracking().ToListAsync();
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
