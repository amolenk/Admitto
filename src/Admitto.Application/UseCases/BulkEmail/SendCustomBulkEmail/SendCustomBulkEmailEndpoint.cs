using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Jobs.SendCustomBulkEmail;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;
using Quartz;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.SendCustomBulkEmail;

/// <summary>
/// Represents an endpoint to send a bulk email.
/// </summary>
public static class SendCustomBulkEmailEndpoint
{
    public static RouteGroupBuilder MapSendCustomBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/custom", SendCustomBulkEmail)
            .WithName(nameof(SendCustomBulkEmail))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Created> SendCustomBulkEmail(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromBody] SendCustomBulkEmailRequest request,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] IApplicationContext context,
        [FromServices] IEmailTemplateService emailTemplateService,
        [FromServices] ISchedulerFactory schedulerFactory,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        await EnsureEmailRecipientListExistsAsync(
            eventId,
            request.RecipientListName,
            context,
            cancellationToken);

        await EnsureEmailTemplateExistsAsync(
            teamId,
            eventId,
            request.EmailType,
            emailTemplateService,
            cancellationToken);

        var idempotencyKey = request.IdempotencyKey is null
            ? Guid.NewGuid()
            : DeterministicGuid.Create(request.IdempotencyKey);

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        var triggerBuilder = TriggerBuilder.Create()
            .ForJob(new JobKey(SendCustomBulkEmailJob.Name))
            .WithIdentity(Guid.NewGuid().ToString(), $"{teamSlug}/{eventSlug}")
            .UsingJobData(SendCustomBulkEmailJob.JobData.TeamId, teamId.ToString())
            .UsingJobData(SendCustomBulkEmailJob.JobData.TicketedEventId, eventId.ToString())
            .UsingJobData(SendCustomBulkEmailJob.JobData.EmailType, request.EmailType)
            .UsingJobData(SendCustomBulkEmailJob.JobData.RecipientListName, request.RecipientListName)
            .UsingJobData(SendCustomBulkEmailJob.JobData.ExcludeAttendees, request.ExcludeAttendees.ToString())
            .UsingJobData(SendCustomBulkEmailJob.JobData.IdempotencyKey, idempotencyKey.ToString())
            .StartNow();

        if (request.TestOptions is not null)
        {
            triggerBuilder
                .UsingJobData(
                    SendCustomBulkEmailJob.JobData.TestRecipient, 
                    request.TestOptions.Recipient.NormalizeEmail())
                .UsingJobData(
                    SendCustomBulkEmailJob.JobData.TestEmailCount,
                    request.TestOptions.MaxEmailCount.ToString())
                // Override the idempotency key for test emails to be able to send multiple messages
                // to the same recipient.
                .UsingJobData(
                    SendCustomBulkEmailJob.JobData.IdempotencyKey,
                    EmailDispatcher.TestMessageIdempotencyKey.ToString());
        }

        await scheduler.ScheduleJob(triggerBuilder.Build(), cancellationToken);
        return TypedResults.Created();
    }

    private static async ValueTask EnsureEmailRecipientListExistsAsync(
        Guid eventId,
        string listName,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var recipientList = await context.EmailRecipientLists
            .AsNoTracking()
            .FirstOrDefaultAsync(
                erl => erl.TicketedEventId == eventId && erl.Name == listName,
                cancellationToken);

        if (recipientList is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailRecipientList.NotFound);
        }
    }

    private static async ValueTask EnsureEmailTemplateExistsAsync(
        Guid teamId,
        Guid eventId,
        string emailType,
        IEmailTemplateService emailTemplateService,
        CancellationToken cancellationToken)
    {
        await emailTemplateService.LoadEmailTemplateAsync(
            emailType,
            teamId,
            eventId,
            cancellationToken);
    }
}