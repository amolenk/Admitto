using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class JobsWorker(
    IServiceProvider serviceProvider,
    IOptions<JobsWorkerOptions> options,
    ILogger<JobsWorker> logger) : BackgroundService
{
    private readonly JobsWorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Jobs worker starting");

        // Reload running jobs state on startup
        await ReloadRunningJobsAsync(stoppingToken);

        var scheduledJobsTask = ProcessScheduledJobsAsync(stoppingToken);
        var orphanedJobsTask = ProcessOrphanedJobsAsync(stoppingToken);

        await Task.WhenAll(scheduledJobsTask, orphanedJobsTask);
    }

    private async Task ProcessScheduledJobsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckScheduledJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing scheduled jobs");
            }

            await Task.Delay(_options.ScheduledJobsCheckInterval, stoppingToken);
        }
    }

    private async Task ProcessOrphanedJobsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOrphanedJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing orphaned jobs");
            }

            await Task.Delay(_options.OrphanedJobsCheckInterval, stoppingToken);
        }
    }

    private async Task CheckScheduledJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();
        var jobRunner = scope.ServiceProvider.GetRequiredService<IJobRunner>();

        var now = DateTimeOffset.UtcNow;
        var dueJobs = await jobContext.ScheduledJobs
            .Where(sj => sj.IsEnabled && sj.NextRunTime <= now)
            .ToListAsync(cancellationToken);

        foreach (var scheduledJob in dueJobs)
        {
            try
            {
                logger.LogInformation("Starting scheduled job {JobId} of type {JobType}", 
                    scheduledJob.Id, scheduledJob.JobType);

                // Deserialize and start the job
                var jobType = Type.GetType(scheduledJob.JobType);
                if (jobType == null)
                {
                    logger.LogError("Job type {JobType} not found for scheduled job {JobId}", 
                        scheduledJob.JobType, scheduledJob.Id);
                    continue;
                }

                var jobInstance = System.Text.Json.JsonSerializer.Deserialize(
                    scheduledJob.JobData, jobType, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });

                if (jobInstance is IJob job)
                {
                    await jobRunner.StartJob(job, cancellationToken);

                    // Update next run time
                    var cronSchedule = CronExpression.Parse(scheduledJob.CronExpression);
                    var nextRunTime = cronSchedule.GetNextOccurrence(now.DateTime, TimeZoneInfo.Utc);
                    if (nextRunTime.HasValue)
                    {
                        var nextRunTimeOffset = new DateTimeOffset(nextRunTime.Value, TimeSpan.Zero);
                        scheduledJob.UpdateNextRunTime(nextRunTimeOffset);
                        await jobContext.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning("Could not calculate next run time for scheduled job {JobId}", 
                            scheduledJob.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting scheduled job {JobId}: {Error}", 
                    scheduledJob.Id, ex.Message);
            }
        }
    }

    private async Task CheckOrphanedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();
        var messageOutbox = scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cutoffTime = DateTimeOffset.UtcNow.Subtract(_options.OrphanedJobThreshold);
        
        var orphanedJobs = await jobContext.Jobs
            .Where(j => j.Status == JobStatus.Running && j.StartedAt < cutoffTime)
            .ToListAsync(cancellationToken);

        foreach (var orphanedJob in orphanedJobs)
        {
            logger.LogWarning("Found orphaned job {JobId}, restarting", orphanedJob.Id);
            
            // Re-enqueue the job for execution
            var jobStartCommand = new StartJobCommand(orphanedJob.Id);
            messageOutbox.Enqueue(jobStartCommand, priority: true);
        }

        if (orphanedJobs.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ReloadRunningJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();

        var runningJobs = await jobContext.Jobs
            .Where(j => j.Status == JobStatus.Running)
            .CountAsync(cancellationToken);

        logger.LogInformation("Found {RunningJobsCount} running jobs to monitor", runningJobs);
    }
}

public class JobsWorkerOptions
{
    public const string SectionName = nameof(JobsWorker);

    public TimeSpan ScheduledJobsCheckInterval { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan OrphanedJobsCheckInterval { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan OrphanedJobThreshold { get; init; } = TimeSpan.FromMinutes(30);
}