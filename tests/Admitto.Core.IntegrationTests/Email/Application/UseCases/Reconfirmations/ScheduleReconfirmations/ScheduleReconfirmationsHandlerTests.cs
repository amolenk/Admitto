using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;

[TestClass]
public sealed class ScheduleReconfirmationsHandlerTests
{
    private static readonly DateTimeOffset Opens = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Closes = new(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

    private async Task<(IScheduler Scheduler, ScheduleReconfirmationsHandler Subject)> CreateAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQuartz(q =>
        {
            q.AddJob<NoopJob>(c => c
                .StoreDurably()
                .WithIdentity(RequestReconfirmationsJob.Name));
        });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ISchedulerFactory>();
        var scheduler = await factory.GetScheduler();
        await scheduler.Start();

        var subject = new ScheduleReconfirmationsHandler(
            factory, NullLogger<ScheduleReconfirmationsHandler>.Instance);
        return (scheduler, subject);
    }

    private static TriggerKey TriggerKeyFor(TicketedEventId eventId) =>
        new(eventId.Value.ToString("N"), ScheduleReconfirmationsHandler.TriggerGroup);

    private static ReconfirmTriggerSpecDto Spec(
        Guid teamId,
        Guid eventId,
        string tz,
        int cadenceHours = 24) =>
        new(teamId, eventId, tz, Opens, Closes, cadenceHours, 24);

    // Given a reconfirm trigger spec with a daily cadence and a time zone
    // When the schedule command is handled
    // Then a cron trigger is created with the expected cron expression, time zone, and job data
    [TestMethod]
    public async Task Upsert_CreatesTriggerWithExpectedCronAndTimeZone()
    {
        var (scheduler, subject) = await CreateAsync();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId.Value, Spec(teamId.Value, eventId.Value, "Europe/Amsterdam")),
            default);

        var trigger = (ICronTrigger?)await scheduler.GetTrigger(TriggerKeyFor(eventId));
        trigger.ShouldNotBeNull();
        trigger.CronExpressionString.ShouldBe("0 0 9 * * ?");
        trigger.TimeZone.Id.ShouldBe("Europe/Amsterdam");
        trigger.JobKey.Name.ShouldBe(RequestReconfirmationsJob.Name);
        trigger.JobDataMap.GetString(RequestReconfirmationsJob.TeamIdKey).ShouldBe(teamId.Value.ToString());
        trigger.JobDataMap.GetString(RequestReconfirmationsJob.TicketedEventIdKey).ShouldBe(eventId.Value.ToString());
        trigger.JobDataMap.GetString(RequestReconfirmationsJob.MinEmailIntervalHoursKey).ShouldBe("24");

        await scheduler.Shutdown();
    }

    // Given a reconfirm trigger spec with a multi-day cadence
    // When the schedule command is handled
    // Then the trigger uses a stepped cron expression for that cadence
    [TestMethod]
    public async Task Upsert_MultiDayCadence_UsesSteppedCron()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId.Value, Spec(Guid.NewGuid(), eventId.Value, "UTC", cadenceHours: 72)),
            default);

        var trigger = (ICronTrigger?)await scheduler.GetTrigger(TriggerKeyFor(eventId));
        trigger.ShouldNotBeNull();
        trigger.CronExpressionString.ShouldBe("0 0 9 1/3 * ?");

        await scheduler.Shutdown();
    }

    // Given a reconfirm trigger spec with a sub-day cadence
    // When the schedule command is handled
    // Then the trigger uses an hourly cron expression for that cadence
    [TestMethod]
    public async Task Upsert_SubDayCadence_UsesHourlyCron()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId.Value, Spec(Guid.NewGuid(), eventId.Value, "UTC", cadenceHours: 6)),
            default);

        var trigger = (ICronTrigger?)await scheduler.GetTrigger(TriggerKeyFor(eventId));
        trigger.ShouldNotBeNull();
        trigger.CronExpressionString.ShouldBe("0 0 0/6 * * ?");

        await scheduler.Shutdown();
    }

    // Given an existing trigger scheduled for an event
    // When the schedule command is handled again with a different time zone
    // Then the existing trigger is replaced with one using the new time zone
    [TestMethod]
    public async Task Upsert_ExistingTrigger_IsReplacedWithNewTimeZone()
    {
        var (scheduler, subject) = await CreateAsync();
        var teamId = Guid.NewGuid();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId.Value, Spec(teamId, eventId.Value, "Europe/Amsterdam")), default);
        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId.Value, Spec(teamId, eventId.Value, "America/New_York")), default);

        var trigger = (ICronTrigger?)await scheduler.GetTrigger(TriggerKeyFor(eventId));
        trigger.ShouldNotBeNull();
        trigger.TimeZone.Id.ShouldBe("America/New_York");

        await scheduler.Shutdown();
    }

    // Given a reconfirm trigger spec with an unrecognized time zone identifier
    // When the schedule command is handled
    // Then no trigger is created and no exception is thrown
    [TestMethod]
    public async Task Upsert_UnknownTimeZone_IsNoOpAndDoesNotThrow()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId.Value, Spec(Guid.NewGuid(), eventId.Value, "Not/AReal_Zone")),
            default);

        (await scheduler.GetTrigger(TriggerKeyFor(eventId))).ShouldBeNull();

        await scheduler.Shutdown();
    }

    // Given a reconfirm trigger spec with an invalid (zero) cadence
    // When the schedule command is handled
    // Then it throws an argument-out-of-range exception
    [TestMethod]
    public async Task Upsert_InvalidCadence_Throws()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            subject.HandleAsync(
                new ScheduleReconfirmationsCommand(eventId.Value, Spec(Guid.NewGuid(), eventId.Value, "UTC", cadenceHours: 0)),
                default).AsTask());

        await scheduler.Shutdown();
    }

    // Given a reconfirm trigger spec whose closes date is before its opens date
    // When the schedule command is handled
    // Then it throws an argument exception
    [TestMethod]
    public async Task Upsert_ClosesBeforeOpens_Throws()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();
        var bad = new ReconfirmTriggerSpecDto(Guid.NewGuid(), eventId.Value, "UTC", Closes, Opens, 1, 24);

        await Should.ThrowAsync<ArgumentException>(() =>
            subject.HandleAsync(new ScheduleReconfirmationsCommand(eventId.Value, bad), default).AsTask());

        await scheduler.Shutdown();
    }

    // Given an event with an existing scheduled trigger
    // When the schedule command is handled with no spec
    // Then the existing trigger is removed
    [TestMethod]
    public async Task Remove_RemovesExistingTrigger()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId.Value, Spec(Guid.NewGuid(), eventId.Value, "UTC")), default);
        (await scheduler.GetTrigger(TriggerKeyFor(eventId))).ShouldNotBeNull();

        await subject.HandleAsync(new ScheduleReconfirmationsCommand(eventId.Value, Spec: null), default);

        (await scheduler.GetTrigger(TriggerKeyFor(eventId))).ShouldBeNull();

        await scheduler.Shutdown();
    }

    // Given an event with no scheduled trigger
    // When the schedule command is handled with no spec
    // Then nothing happens and no exception is thrown
    [TestMethod]
    public async Task Remove_AbsentTrigger_IsNoOp()
    {
        var (scheduler, subject) = await CreateAsync();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(TicketedEventId.New().Value, Spec: null),
            default);

        await scheduler.Shutdown();
    }

    private sealed class NoopJob : IJob
    {
        public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
    }
}
