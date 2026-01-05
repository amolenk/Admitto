using System.Runtime.CompilerServices;
using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;
using Quartz;

namespace Amolenk.Admitto.Application.Jobs.SendCustomBulkEmail;

public class SendCustomBulkEmailJob(
    IApplicationContext context,
    CustomEmailComposer emailComposer,
    IEmailDispatcher dispatcher,
    ILogger<SendCustomBulkEmailJob> logger)
    : IJob
{
    public const string Name = nameof(SendCustomBulkEmailJob);

    public static class JobData
    {
        public const string TeamId = "teamId";
        public const string TicketedEventId = "eventId";
        public const string EmailType = "emailType";
        public const string RecipientListName = "recipientListName";
        public const string IdempotencyKey = "idempotencyKey";
        public const string ExcludeAttendees = "excludeAttendees";
        public const string TestRecipient = "testRecipient";
        public const string TestEmailCount = "maxEmailCount";
    }

    public async Task Execute(IJobExecutionContext jobExecutionContext)
    {
        try
        {
            var teamId = jobExecutionContext.MergedJobDataMap.GetGuidValueFromString(JobData.TeamId);
            var eventId = jobExecutionContext.MergedJobDataMap.GetGuidValueFromString(JobData.TicketedEventId);
            var emailType = jobExecutionContext.MergedJobDataMap.GetString(JobData.EmailType)!;
            var recipientListName = jobExecutionContext.MergedJobDataMap.GetString(JobData.RecipientListName);
            var idempotencyKey = jobExecutionContext.MergedJobDataMap.GetGuidValueFromString(JobData.IdempotencyKey);
            var excludeAttendees = jobExecutionContext.MergedJobDataMap.GetBooleanValueFromString(JobData.ExcludeAttendees);
            
            jobExecutionContext.MergedJobDataMap.TryGetString(JobData.TestRecipient, out var testRecipient);
            jobExecutionContext.MergedJobDataMap.TryGetIntValueFromString(JobData.TestEmailCount, out var testEmailCount);

            if (testRecipient is not null)
            {
                logger.LogInformation("Sending test {EmailType} bulk email to {testRecipient}...",
                    emailType, 
                    recipientListName);
            }
            else
            {
                logger.LogInformation("Sending {EmailType} bulk email to recipient list {RecipientListName}...",
                    emailType, 
                    recipientListName);
            }

            var recipients = await GetRecipientListAsync(
                eventId,
                recipientListName!,
                excludeAttendees,
                testRecipient,
                testEmailCount,
                jobExecutionContext.CancellationToken);

            var emailMessages = ComposeEmailsAsync(
                teamId,
                eventId,
                emailType,
                recipients,
                jobExecutionContext.CancellationToken);

            await dispatcher.DispatchEmailsAsync(
                emailMessages,
                teamId,
                eventId,
                idempotencyKey,
                jobExecutionContext.CancellationToken);
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }

    private async IAsyncEnumerable<EmailMessage> ComposeEmailsAsync(
        Guid teamId,
        Guid ticketedEventId,
        string emailType,
        IEnumerable<EmailRecipient> recipients,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var defaultTemplateParameters = await GetDefaultParametersAsync(
            ticketedEventId,
            cancellationToken);

        foreach (var recipient in recipients)
        {
            var templateParameters = new Dictionary<string, object>(defaultTemplateParameters);

            foreach (var detail in recipient.Details)
            {
                templateParameters[detail.Name] = detail.Value;
            }

            yield return await emailComposer.ComposeMessageAsync(
                teamId,
                ticketedEventId,
                emailType,
                recipient.Email,
                templateParameters,
                cancellationToken: cancellationToken);
        }
    }

    private async ValueTask<List<EmailRecipient>> GetRecipientListAsync(
        Guid ticketedEventId,
        string name,
        bool excludeAttendees,
        string? testRecipient,
        int testEmailCount,
        CancellationToken cancellationToken = default)
    {
        var recipientList = await context.EmailRecipientLists
            .AsNoTracking()
            .FirstOrDefaultAsync(
                erl => erl.TicketedEventId == ticketedEventId && erl.Name == name,
                cancellationToken);

        if (recipientList is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailRecipientList.NotFound);
        }
        
        var recipients = recipientList.Recipients.ToList();

        if (excludeAttendees)
        {
            var attendeeEmails = await context.Participants
                .AsNoTracking()
                .Where(p => p.TicketedEventId == ticketedEventId)
                .Select(p => p.Email)
                .ToListAsync(cancellationToken);

            recipients.RemoveAll(r => attendeeEmails.Contains(r.Email));
        }
        
        if (testRecipient is null) return recipients;
        
        var testRecipients = new List<EmailRecipient>();
        for (var i = 0; i < Math.Min(testEmailCount, recipients.Count); i++)
        {
            var originalRecipient = recipients[i];

            testRecipients.Add(
                new EmailRecipient
                {
                    Email = testRecipient,
                    Details = originalRecipient.Details
                });
        }

        return testRecipients;
    }

    private async ValueTask<Dictionary<string, object>> GetDefaultParametersAsync(
        Guid ticketedEventId,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents
            .AsNoTracking()
            .Where(x => x.Id == ticketedEventId)
            .Select(x => new
            {
                x.Name,
                x.Website,
                // x.BaseUrl,
                // x.TicketTypes
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        var defaultParameters = new Dictionary<string, object>
        {
            ["event_name"] = ticketedEvent.Name,
            ["event_website"] = ticketedEvent.Website
        };

        return defaultParameters;
    }
}