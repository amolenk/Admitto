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

        // If no reconfirmation policy is set, make sure no job is scheduled.
        var policy = ticketedEvent.ReconfirmPolicy;
        if (policy is null)
        {
            // TODO Check that we can call this if the job does not exist.
            await scheduler.UnscheduleJob(triggerKey, cancellationToken);
            return;
        }

        // Otherwise, create the new trigger (possibly overwriting any existing).
        var triggerBuilder = TriggerBuilder.Create()
            .ForJob(new JobKey(SendReconfirmBulkEmailJob.Name))
            .WithIdentity(triggerKey)
            .UsingJobData(
                SendReconfirmBulkEmailJob.JobData.InitialDelayAfterRegistration,
                policy.InitialDelayAfterRegistration.ToString())
            .UsingJobData(SendReconfirmBulkEmailJob.JobData.ReminderInterval, policy.ReminderInterval.ToString())
            .StartAt(ticketedEvent.StartsAt - policy.WindowStartBeforeEvent)
            .EndAt(ticketedEvent.StartsAt - policy.WindowEndBeforeEvent)
            .WithCronSchedule("0 0 8,12,16,20 ? * *") // every 4 hours between 08:00 and 20:00 (inclusive)
            .StartNow();

        await scheduler.ScheduleJob(triggerBuilder.Build(), cancellationToken);
    }
}