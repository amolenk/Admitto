using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application.Jobs;

internal interface IReconfirmPolicyCloseScheduler
{
    Task ScheduleAsync(
        TicketedEventId ticketedEventId,
        DateTimeOffset closesAt,
        CancellationToken cancellationToken);

    Task UnscheduleAsync(
        TicketedEventId ticketedEventId,
        DateTimeOffset closesAt,
        CancellationToken cancellationToken);
}

/// <summary>
/// Maintains one-shot policy-close triggers. Routine reconfirm evaluation remains
/// on the single fixed hourly trigger; these triggers only guarantee a terminal
/// evaluation when a policy closes between hourly ticks.
/// </summary>
internal sealed class ReconfirmPolicyCloseScheduler(ISchedulerFactory schedulerFactory)
    : IReconfirmPolicyCloseScheduler
{
    public async Task ScheduleAsync(
        TicketedEventId ticketedEventId,
        DateTimeOffset closesAt,
        CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = RequestReconfirmationsJob.PolicyCloseTriggerKey(ticketedEventId, closesAt);
        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(new JobKey(RequestReconfirmationsJob.Name))
            .UsingJobData(
                RequestReconfirmationsJob.PolicyCloseEventIdKey,
                ticketedEventId.Value.ToString())
            .UsingJobData(
                RequestReconfirmationsJob.PolicyCloseAtKey,
                closesAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture))
            .StartAt(closesAt)
            .WithSimpleSchedule(schedule => schedule
                .WithRepeatCount(0)
                .WithMisfireHandlingInstructionFireNow())
            .Build();

        if (await scheduler.CheckExists(triggerKey, cancellationToken))
        {
            await scheduler.RescheduleJob(triggerKey, trigger, cancellationToken);
            return;
        }

        await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    public async Task UnscheduleAsync(
        TicketedEventId ticketedEventId,
        DateTimeOffset closesAt,
        CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = RequestReconfirmationsJob.PolicyCloseTriggerKey(ticketedEventId, closesAt);
        if (await scheduler.CheckExists(triggerKey, cancellationToken))
            await scheduler.UnscheduleJob(triggerKey, cancellationToken);
    }
}
