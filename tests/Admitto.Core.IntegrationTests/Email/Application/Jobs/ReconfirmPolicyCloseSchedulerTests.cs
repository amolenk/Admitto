using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Jobs;

[TestClass]
public sealed class ReconfirmPolicyCloseSchedulerTests
{
    // Given a policy closing at a non-hour UTC boundary
    // When its terminal trigger is scheduled
    // Then Quartz receives a one-shot trigger at the exact close instant
    [TestMethod]
    public async Task ScheduleAsync_NonHourPolicyClose_UsesExactOneShotTrigger()
    {
        var scheduler = Substitute.For<IScheduler>();
        var schedulerFactory = Substitute.For<ISchedulerFactory>();
        schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(scheduler);
        scheduler.CheckExists(Arg.Any<TriggerKey>(), Arg.Any<CancellationToken>()).Returns(false);
        var closesAt = new DateTimeOffset(2030, 6, 1, 12, 17, 31, TimeSpan.Zero);
        var eventId = TicketedEventId.New();
        var sut = new ReconfirmPolicyCloseScheduler(schedulerFactory);
        ITrigger? scheduledTrigger = null;

        scheduler.ScheduleJob(
                Arg.Do<ITrigger>(trigger => scheduledTrigger = trigger),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(DateTimeOffset.UtcNow));
        await sut.ScheduleAsync(eventId, closesAt, CancellationToken.None);

        await scheduler.Received(1).ScheduleJob(
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
        scheduledTrigger.ShouldNotBeNull();
        scheduledTrigger.Key.ShouldBe(RequestReconfirmationsJob.PolicyCloseTriggerKey(eventId, closesAt));
        scheduledTrigger.StartTimeUtc.ShouldBe(closesAt);
        scheduledTrigger.ShouldBeAssignableTo<ISimpleTrigger>().RepeatCount.ShouldBe(0);
        scheduledTrigger.JobDataMap.GetString(RequestReconfirmationsJob.PolicyCloseEventIdKey)
            .ShouldBe(eventId.Value.ToString());
        scheduledTrigger.JobDataMap.GetString(RequestReconfirmationsJob.PolicyCloseAtKey)
            .ShouldBe(closesAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
    }
}
