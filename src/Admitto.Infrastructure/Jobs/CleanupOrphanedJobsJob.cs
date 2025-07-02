using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class CleanupOrphanedJobsJob : IJob
{
    public Guid Id { get; } = Guid.NewGuid();
}

public class CleanupOrphanedJobsJobHandler(
    IJobContext jobContext,
    IMessageOutbox messageOutbox,
    IUnitOfWork unitOfWork,
    IOptions<JobsWorkerOptions> options,
    ILogger<CleanupOrphanedJobsJobHandler> logger) : IJobHandler<CleanupOrphanedJobsJob>
{
    private readonly JobsWorkerOptions _options = options.Value;

    public async ValueTask Handle(CleanupOrphanedJobsJob job, IJobProgress jobProgress, CancellationToken cancellationToken = default)
    {
        await jobProgress.ReportProgressAsync("Starting orphaned jobs cleanup", 0, cancellationToken);

        var cutoffTime = DateTimeOffset.UtcNow.Subtract(_options.OrphanedJobThreshold);
        
        var orphanedJobs = await jobContext.Jobs
            .Where(j => j.Status == JobStatus.Running && j.StartedAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (orphanedJobs.Count == 0)
        {
            await jobProgress.ReportProgressAsync("No orphaned jobs found", 100, cancellationToken);
            return;
        }

        await jobProgress.ReportProgressAsync($"Found {orphanedJobs.Count} orphaned jobs, re-enqueueing", 50, cancellationToken);

        foreach (var orphanedJob in orphanedJobs)
        {
            logger.LogWarning("Found orphaned job {JobId}, restarting", orphanedJob.Id);
            
            // Re-enqueue the job for execution
            var jobStartCommand = new StartJobCommand(orphanedJob.Id);
            messageOutbox.Enqueue(jobStartCommand, priority: true);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        await jobProgress.ReportProgressAsync($"Re-enqueued {orphanedJobs.Count} orphaned jobs", 100, cancellationToken);
    }
}