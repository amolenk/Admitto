using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Domain.ValueObjects;
using Quartz;

namespace Amolenk.Admitto.Application.Jobs.SendReconfirmBulkEmail;

public record UnreconfirmedAttendee(
    Guid PublicId,
    Guid AttendeeId,
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetail> AdditionalDetails,
    List<TicketSelection> Tickets,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? ReconfirmEmailSentAt);

public class SendReconfirmBulkEmailJob(
    IApplicationContext context,
    ILogger<SendReconfirmBulkEmailJob> logger)
    : IJob
{
    public const string Name = nameof(SendReconfirmBulkEmailJob);

    public static class JobData
    {
        public const string TeamId = "teamId";
        public const string TicketedEventId = "eventId";
        public const string InitialDelayAfterRegistration = "initialDelayAfterRegistration";
        public const string ReminderInterval = "reminderInterval";
    }

    public async Task Execute(IJobExecutionContext jobExecutionContext)
    {
        try
        {
            var teamId = jobExecutionContext.MergedJobDataMap.GetGuidValueFromString(JobData.TeamId);
            var eventId = jobExecutionContext.MergedJobDataMap.GetGuidValueFromString(JobData.TicketedEventId);
            var initialDelayAfterRegistration =
                jobExecutionContext.MergedJobDataMap.GetTimeSpanValueFromString(JobData.InitialDelayAfterRegistration);
        
            var reminderIntervalSet = jobExecutionContext.MergedJobDataMap.TryGetTimeSpanValueFromString(
                JobData.ReminderInterval,
                out var reminderInterval);

            var now = DateTime.UtcNow;
            var recipients =
                (await GetUnreconfirmedAttendeesAsync(eventId, jobExecutionContext.CancellationToken))
                .Where(a => ShouldSendReconfirmEmail(
                    now,
                    a.RegisteredAt,
                    a.ReconfirmEmailSentAt,
                    initialDelayAfterRegistration,
                    reminderInterval))
                .ToList();
        
            logger.LogInformation(
                "Sending reconfirmation emails to {RecipientCount} recipients(s)...",
                recipients.Count);
        
        
            // var emailMessages = ComposeEmailsAsync(
            //     teamId,
            //     eventId,
            //     emailType,
            //     recipients,
            //     jobExecutionContext.CancellationToken);
            //
            // await dispatcher.DispatchEmailsAsync(
            //     emailMessages,
            //     teamId,
            //     eventId,
            //     idempotencyKey,
            //     jobExecutionContext.CancellationToken);
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }

    private async ValueTask<List<UnreconfirmedAttendee>> GetUnreconfirmedAttendeesAsync(
        Guid ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        return await context.ParticipationView
            .AsNoTracking()
            .Where(p => p.TicketedEventId == ticketedEventId &&
                        p.AttendeeStatus == ParticipationAttendeeStatus.Registered)
            .Join(
                context.Attendees,
                p => p.AttendeeId,
                a => a.Id,
                (p, a) => new UnreconfirmedAttendee(
                    p.PublicId,
                    p.AttendeeId!.Value,
                    p.Email,
                    a.FirstName,
                    a.LastName,
                    a.AdditionalDetails.ToList(),
                    a.Tickets.ToList(),
                    a.CreatedAt,
                    context.EmailLog
                        .Where(el => el.Recipient == p.Email && el.EmailType == WellKnownEmailType.Reconfirm)
                        .OrderByDescending(el => el.SentAt)
                        .Select(el => el.SentAt)
                        .FirstOrDefault()
                ))
            .ToListAsync(cancellationToken);
    }

    private static bool ShouldSendReconfirmEmail(
        DateTimeOffset now,
        DateTimeOffset attendeeRegisteredAt,
        DateTimeOffset? latestReconfirmEmailSentAt,
        TimeSpan initialDelayAfterRegistration,
        TimeSpan? reminderInterval)
    {
        if (now < attendeeRegisteredAt + initialDelayAfterRegistration)
            return false;

        if (latestReconfirmEmailSentAt is null)
            return true;

        if (reminderInterval is null)
            return false;

        return latestReconfirmEmailSentAt + reminderInterval <= now;
    }
}