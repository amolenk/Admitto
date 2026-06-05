using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.TriggerBulkEmailJob;

/// <summary>
/// Turns a freshly-created <see cref="Domain.Entities.BulkEmailJob"/> into a one-shot
/// Quartz schedule. Each bulk job gets its own <see cref="JobKey"/> so that
/// <see cref="DisallowConcurrentExecutionAttribute"/> only blocks parallel pickups
/// of the same job, not unrelated bulk jobs (D10).
/// </summary>
internal sealed class TriggerBulkEmailJobHandler(
    ISchedulerFactory schedulerFactory,
    ILogger<TriggerBulkEmailJobHandler> logger)
    : ICommandHandler<TriggerBulkEmailJobCommand>, IWorkerOnly
{
    public const string JobGroup = "bulk-email-fan-out";

    public async ValueTask HandleAsync(
        TriggerBulkEmailJobCommand command,
        CancellationToken cancellationToken)
    {
        BulkEmailJobId bulkEmailJobId = BulkEmailJobId.From(command.BulkEmailJobId);

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        var jobKey = new JobKey(bulkEmailJobId.Value.ToString("N"), JobGroup);
        var triggerKey = new TriggerKey($"{jobKey.Name}.trigger", JobGroup);

        if (await scheduler.CheckExists(jobKey, cancellationToken))
        {
            // Idempotency: outbox redelivery should not double-schedule.
            logger.LogDebug(
                "Bulk-email fan-out job {JobKey} already scheduled; skipping",
                jobKey);
            return;
        }

        var jobDetail = JobBuilder.Create<SendBulkEmailJob>()
            .WithIdentity(jobKey)
            .UsingJobData(SendBulkEmailJob.BulkEmailJobIdKey, bulkEmailJobId.Value.ToString())
            .StoreDurably(false)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobDetail)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
    }
}
