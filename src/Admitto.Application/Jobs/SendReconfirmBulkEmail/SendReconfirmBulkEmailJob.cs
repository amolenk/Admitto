using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.UseCases.BulkEmail.SendReconfirmBulkEmail;
using Amolenk.Admitto.Domain.Utilities;
using Quartz;

namespace Amolenk.Admitto.Application.Jobs.SendReconfirmBulkEmail;

/// <summary>
/// Represents a job that sends reconfirmation emails in bulk to attendees of a ticketed event.
/// </summary>
public class SendReconfirmBulkEmailJob(ICommandSender commandSender)
    : IJob
{
    public const string Name = nameof(SendReconfirmBulkEmailJob);

    public static class JobData
    {
        public const string TeamId = nameof(TeamId);
        public const string TicketedEventId = nameof(TicketedEventId);
        public const string InitialDelayAfterRegistration = nameof(InitialDelayAfterRegistration);
        public const string ReminderInterval = nameof(ReminderInterval);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var teamId = context.MergedJobDataMap.GetGuidValueFromString(JobData.TeamId);
            var eventId = context.MergedJobDataMap.GetGuidValueFromString(JobData.TicketedEventId);
            var initialDelayAfterRegistration =
                context.MergedJobDataMap.GetTimeSpanValueFromString(JobData.InitialDelayAfterRegistration);

            context.MergedJobDataMap.TryGetTimeSpanValueFromString(
                JobData.ReminderInterval,
                out var reminderInterval);

            var sendReconfirmBulkEmailCommand = new SendReconfirmBulkEmailCommand(
                teamId,
                eventId,
                initialDelayAfterRegistration,
                reminderInterval)
            {
                CommandId = DeterministicGuid.Create(context.FireInstanceId)
            };

            await commandSender.SendAsync(sendReconfirmBulkEmailCommand, context.CancellationToken);
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }
}