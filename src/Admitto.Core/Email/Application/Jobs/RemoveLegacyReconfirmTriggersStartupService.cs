using Quartz;
using Quartz.Impl.Matchers;

namespace Amolenk.Admitto.Core.Email.Application.Jobs;

/// <summary>
/// Removes persistent per-event reconfirm triggers left by older deployments.
/// The cleanup is idempotent and runs before the stable hourly evaluator can
/// observe another legacy trigger.
/// </summary>
internal sealed class RemoveLegacyReconfirmTriggersStartupService(
    ISchedulerFactory schedulerFactory,
    ILogger<RemoveLegacyReconfirmTriggersStartupService> logger)
    : BackgroundService
{
    internal const string LegacyTriggerGroup = "reconfirm";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
            var keys = await scheduler.GetTriggerKeys(
                GroupMatcher<TriggerKey>.GroupEquals(LegacyTriggerGroup),
                stoppingToken);

            foreach (var key in keys)
                await scheduler.UnscheduleJob(key, stoppingToken);

            if (keys.Count > 0)
                logger.LogInformation("Removed {Count} legacy reconfirm trigger(s).", keys.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Legacy reconfirm trigger cleanup failed.");
        }
    }
}
