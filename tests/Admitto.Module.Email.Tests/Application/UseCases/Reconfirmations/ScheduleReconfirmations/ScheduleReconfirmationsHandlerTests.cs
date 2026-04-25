using Amolenk.Admitto.Module.Email.Application.Jobs;
using Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.Reconfirmations.ScheduleReconfirmations;

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

    private static ReconfirmTriggerSpecDto Spec(Guid teamId, Guid eventId, string tz, int cadenceDays = 1) =>
        new(teamId, eventId, tz, Opens, Closes, cadenceDays);

    [TestMethod]
    public async Task Upsert_CreatesTriggerWithExpectedCronAndTimeZone()
    {
        var (scheduler, subject) = await CreateAsync();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId, Spec(teamId.Value, eventId.Value, "Europe/Amsterdam")),
            default);

        var trigger = (ICronTrigger?)await scheduler.GetTrigger(TriggerKeyFor(eventId));
        trigger.ShouldNotBeNull();
        trigger.CronExpressionString.ShouldBe("0 0 9 * * ?");
        trigger.TimeZone.Id.ShouldBe("Europe/Amsterdam");
        trigger.JobKey.Name.ShouldBe(RequestReconfirmationsJob.Name);
        trigger.JobDataMap.GetString(RequestReconfirmationsJob.TeamIdKey).ShouldBe(teamId.Value.ToString());
        trigger.JobDataMap.GetString(RequestReconfirmationsJob.TicketedEventIdKey).ShouldBe(eventId.Value.ToString());

        await scheduler.Shutdown();
    }

    [TestMethod]
    public async Task Upsert_MultiDayCadence_UsesSteppedCron()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId, Spec(Guid.NewGuid(), eventId.Value, "UTC", cadenceDays: 3)),
            default);

        var trigger = (ICronTrigger?)await scheduler.GetTrigger(TriggerKeyFor(eventId));
        trigger.ShouldNotBeNull();
        trigger.CronExpressionString.ShouldBe("0 0 9 1/3 * ?");

        await scheduler.Shutdown();
    }

    [TestMethod]
    public async Task Upsert_ExistingTrigger_IsReplacedWithNewTimeZone()
    {
        var (scheduler, subject) = await CreateAsync();
        var teamId = Guid.NewGuid();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId, Spec(teamId, eventId.Value, "Europe/Amsterdam")), default);
        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId, Spec(teamId, eventId.Value, "America/New_York")), default);

        var trigger = (ICronTrigger?)await scheduler.GetTrigger(TriggerKeyFor(eventId));
        trigger.ShouldNotBeNull();
        trigger.TimeZone.Id.ShouldBe("America/New_York");

        await scheduler.Shutdown();
    }

    [TestMethod]
    public async Task Upsert_UnknownTimeZone_IsNoOpAndDoesNotThrow()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId, Spec(Guid.NewGuid(), eventId.Value, "Not/AReal_Zone")),
            default);

        (await scheduler.GetTrigger(TriggerKeyFor(eventId))).ShouldBeNull();

        await scheduler.Shutdown();
    }

    [TestMethod]
    public async Task Upsert_InvalidCadence_Throws()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            subject.HandleAsync(
                new ScheduleReconfirmationsCommand(eventId, Spec(Guid.NewGuid(), eventId.Value, "UTC", cadenceDays: 0)),
                default).AsTask());

        await scheduler.Shutdown();
    }

    [TestMethod]
    public async Task Upsert_ClosesBeforeOpens_Throws()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();
        var bad = new ReconfirmTriggerSpecDto(Guid.NewGuid(), eventId.Value, "UTC", Closes, Opens, 1);

        await Should.ThrowAsync<ArgumentException>(() =>
            subject.HandleAsync(new ScheduleReconfirmationsCommand(eventId, bad), default).AsTask());

        await scheduler.Shutdown();
    }

    [TestMethod]
    public async Task Remove_RemovesExistingTrigger()
    {
        var (scheduler, subject) = await CreateAsync();
        var eventId = TicketedEventId.New();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(eventId, Spec(Guid.NewGuid(), eventId.Value, "UTC")), default);
        (await scheduler.GetTrigger(TriggerKeyFor(eventId))).ShouldNotBeNull();

        await subject.HandleAsync(new ScheduleReconfirmationsCommand(eventId, Spec: null), default);

        (await scheduler.GetTrigger(TriggerKeyFor(eventId))).ShouldBeNull();

        await scheduler.Shutdown();
    }

    [TestMethod]
    public async Task Remove_AbsentTrigger_IsNoOp()
    {
        var (scheduler, subject) = await CreateAsync();

        await subject.HandleAsync(
            new ScheduleReconfirmationsCommand(TicketedEventId.New(), Spec: null),
            default);

        await scheduler.Shutdown();
    }

    private sealed class NoopJob : IJob
    {
        public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
    }
}
