using Amolenk.Admitto.Application.Common.Data;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Jobs.SendReconfirmBulkEmail;
using Quartz;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleReconfirmBulkEmail;

/// <summary>
/// Represents a command handler that schedules a reconfirmation bulk email for a ticketed event.
/// </summary>
public class ScheduleReconfirmBulkEmailHandler(IApplicationContext context, ISchedulerFactory schedulerFactory)
    : ICommandHandler<ScheduleReconfirmBulkEmailCommand>
{
    public async ValueTask HandleAsync(ScheduleReconfirmBulkEmailCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent =
            await context.TicketedEvents.GetWithoutTrackingAsync(command.TicketedEventId, cancellationToken);

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = new TriggerKey(
            "SendReconfirmBulkEmail",
            $"{command.TeamId}/{command.TicketedEventId}");

        // Remove any existing trigger.
        // TODO Check that we can call this if the job does not exist.
        await scheduler.UnscheduleJob(triggerKey, cancellationToken);

        // If no reconfirmation policy is set, make sure no job is scheduled.
        var policy = ticketedEvent.ReconfirmPolicy;
        if (policy is null)
        {
            return;
        }

        // Otherwise, create the new trigger (possibly overwriting any existing).
        var trigger = TriggerBuilder.Create()
            .ForJob(new JobKey(SendReconfirmBulkEmailJob.Name))
            .WithIdentity(triggerKey)
            .UsingJobData(SendReconfirmBulkEmailJob.JobData.TeamId, command.TeamId.ToString())
            .UsingJobData(SendReconfirmBulkEmailJob.JobData.TicketedEventId, command.TicketedEventId.ToString())
            .UsingJobData(
                SendReconfirmBulkEmailJob.JobData.InitialDelayAfterRegistration,
                policy.InitialDelayAfterRegistration.ToString())
            .UsingJobData(SendReconfirmBulkEmailJob.JobData.ReminderInterval, policy.ReminderInterval.ToString())
            .StartAt(ticketedEvent.StartsAt - policy.WindowStartBeforeEvent)
            .EndAt(ticketedEvent.StartsAt - policy.WindowEndBeforeEvent)
            // every 4 hours between 08:00 and 20:00 (inclusive)
            .WithCronSchedule(
                "0 0 8,12,16,20 ? * *",
                // TODO Get time zone from specific event.
                options => options.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam")))
            .Build();

        await scheduler.ScheduleJob(trigger, cancellationToken);
    }
}