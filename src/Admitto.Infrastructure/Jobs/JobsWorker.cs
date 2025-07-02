using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
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
    private readonly ConcurrentDictionary<Guid, Task> _runningJobs = new();
    private readonly SemaphoreSlim _jobExecutionSemaphore = new(options.Value.MaxConcurrentJobs, options.Value.MaxConcurrentJobs);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Jobs worker starting with max {MaxConcurrentJobs} concurrent jobs", _options.MaxConcurrentJobs);

        // Reload running jobs state on startup
        await ReloadRunningJobsAsync(stoppingToken);

        var scheduledJobsTask = ProcessScheduledJobsAsync(stoppingToken);
        var orphanedJobsTask = ProcessOrphanedJobsAsync(stoppingToken);
        var pendingJobsTask = ProcessPendingJobsAsync(stoppingToken);

        await Task.WhenAll(scheduledJobsTask, orphanedJobsTask, pendingJobsTask);
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

    private async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing pending jobs");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    public async ValueTask<bool> TryExecuteJob(Guid jobId, CancellationToken cancellationToken = default)
    {
        // Check if we have capacity
        if (!await _jobExecutionSemaphore.WaitAsync(0, cancellationToken))
        {
            logger.LogDebug("Cannot execute job {JobId} - max concurrent jobs reached", jobId);
            return false;
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var job = await jobContext.Jobs
                .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

            if (job == null)
            {
                logger.LogWarning("Job {JobId} not found", jobId);
                return false;
            }

            if (job.Status != JobStatus.Pending)
            {
                logger.LogWarning("Job {JobId} is not in pending status (current: {Status})", 
                    jobId, job.Status);
                return false;
            }

            // Start the job execution in background
            var executionTask = ExecuteJobInBackgroundAsync(jobId);
            _runningJobs[jobId] = executionTask;

            // Clean up completed task
            _ = executionTask.ContinueWith(t =>
            {
                _runningJobs.TryRemove(jobId, out _);
                _jobExecutionSemaphore.Release();
            }, TaskScheduler.Default);

            return true;
        }
        catch
        {
            _jobExecutionSemaphore.Release();
            throw;
        }
    }

    private async Task CheckPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();

        var pendingJobs = await jobContext.Jobs
            .Where(j => j.Status == JobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .Take(10) // Process up to 10 pending jobs at a time
            .ToListAsync(cancellationToken);

        foreach (var job in pendingJobs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await TryExecuteJob(job.Id, cancellationToken);
        }
    }

    private async Task ExecuteJobInBackgroundAsync(Guid jobId)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var job = await jobContext.Jobs
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                logger.LogWarning("Job {JobId} not found during execution", jobId);
                return;
            }

            if (job.Status != JobStatus.Pending)
            {
                logger.LogWarning("Job {JobId} is not in pending status during execution (current: {Status})", 
                    jobId, job.Status);
                return;
            }

            try
            {
                job.Start();
                await unitOfWork.SaveChangesAsync();

                logger.LogInformation("Starting job {JobId} of type {JobType}", job.Id, job.JobType);

                // Deserialize and execute the job
                await ExecuteJobAsync(job, scope.ServiceProvider);

                job.Complete();
                await unitOfWork.SaveChangesAsync();

                logger.LogInformation("Job {JobId} completed successfully", job.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Job {JobId} failed with error: {Error}", job.Id, ex.Message);

                job.Fail(ex.Message);
                await unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error executing job {JobId}: {Error}", jobId, ex.Message);
        }
    }

    private async ValueTask ExecuteJobAsync(Job jobEntity, IServiceProvider scopedServiceProvider)
    {
        // Get the job type
        var jobType = Type.GetType(jobEntity.JobType);
        if (jobType == null)
        {
            throw new InvalidOperationException($"Job type {jobEntity.JobType} not found");
        }

        // Deserialize the job data
        var jobInstance = JsonSerializer.Deserialize(jobEntity.JobData, jobType, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (jobInstance is not IJob job)
        {
            throw new InvalidOperationException($"Job type {jobEntity.JobType} does not implement IJob");
        }

        // Find the handler type
        var handlerType = typeof(IJobHandler<>).MakeGenericType(jobType);
        var handler = scopedServiceProvider.GetRequiredService(handlerType);

        // Create job progress tracker
        var unitOfWork = scopedServiceProvider.GetRequiredService<IUnitOfWork>();
        var jobProgress = new JobProgress(jobEntity, unitOfWork, logger);

        // Get the Handle method and invoke it
        var handleMethod = handlerType.GetMethod("Handle");
        if (handleMethod == null)
        {
            throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");
        }

        var result = handleMethod.Invoke(handler, [job, jobProgress, CancellationToken.None]);
        
        if (result is ValueTask valueTask)
        {
            await valueTask;
        }
        else if (result is Task task)
        {
            await task;
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
    public int MaxConcurrentJobs { get; init; } = 5;
}