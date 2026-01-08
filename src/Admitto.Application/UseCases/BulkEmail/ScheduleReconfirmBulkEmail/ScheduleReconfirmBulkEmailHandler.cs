using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Jobs.SendReconfirmBulkEmail;
using Quartz;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleReconfirmBulkEmail;

/// <summary>
/// Represents a command handler that schedules a reconfirmation bulk email for a ticketed event.
/// </summary>
public class ScheduleReconfirmBulkEmailHandler(IApplicationContext context, ISchedulerFactory schedulerFactory)
    : ICommandHandler<ScheduleReconfirmBulkEmailCommand>, IWorkerHandler
{
    public async ValueTask HandleAsync(ScheduleReconfirmBulkEmailCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents
            .AsNoTracking()
            .Where(te => te.Id == command.TicketedEventId)
            .Select(te => new
            {
                te.StartsAt,
                te.ReconfirmPolicy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        var policy = ticketedEvent.ReconfirmPolicy;

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = new TriggerKey(
            "SendReconfirmBulkEmail",
            $"{command.TeamId}/{command.TicketedEventId}");

        // If no reconfirmation policy is set, make sure no job is scheduled.
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
            .WithCronSchedule("0 0 9 ? * *") // every day at 09:00
            .StartNow();

        await scheduler.ScheduleJob(triggerBuilder.Build(), cancellationToken);
    }
}