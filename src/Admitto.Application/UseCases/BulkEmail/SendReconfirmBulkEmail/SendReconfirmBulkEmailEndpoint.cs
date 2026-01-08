using Amolenk.Admitto.Application.Jobs.SendReconfirmBulkEmail;
using Amolenk.Admitto.Domain.ValueObjects;
using Quartz;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.SendReconfirmBulkEmail;

/// <summary>
/// Represents an endpoint to send a reconfirm bulk email.
/// </summary>
public static class SendReconfirmBulkEmailEndpoint
{
    public static RouteGroupBuilder MapSendReconfirmBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/reconfirm", SendReconfirmBulkEmail)
            .WithName(nameof(SendReconfirmBulkEmail))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Created> SendReconfirmBulkEmail(
        string teamSlug,
        string eventSlug,
        SendReconfirmBulkEmailRequest request,
        ISlugResolver slugResolver,
        ISchedulerFactory schedulerFactory,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = new TriggerKey($"adhoc:{Guid.NewGuid()}", $"{teamId}/{eventId}");

        var triggerBuilder = TriggerBuilder.Create()
            .ForJob(new JobKey(SendReconfirmBulkEmailJob.Name))
            .WithIdentity(triggerKey)
            .UsingJobData(SendReconfirmBulkEmailJob.JobData.TeamId, teamId.ToString())
            .UsingJobData(SendReconfirmBulkEmailJob.JobData.TicketedEventId, eventId.ToString())
            .UsingJobData(
                SendReconfirmBulkEmailJob.JobData.InitialDelayAfterRegistration,
                request.InitialDelayAfterRegistration.ToString())
            .UsingJobData(SendReconfirmBulkEmailJob.JobData.ReminderInterval, request.ReminderInterval?.ToString()!)
            .StartNow();
        
        await scheduler.ScheduleJob(triggerBuilder.Build(), cancellationToken);
        return TypedResults.Created();
    }
}