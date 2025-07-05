using System.Text.Json;
using System.Threading.Channels;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class JobsWorker(IServiceProvider serviceProvider, IOptions<JobsOptions> options, ILogger<JobsWorker> logger)
    : BackgroundService
{
    private readonly JobsOptions _options = options.Value;
    private readonly Channel<Guid> _jobChannel = Channel.CreateUnbounded<Guid>();
    
    public async ValueTask EnqueueJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await _jobChannel.Writer.WriteAsync(jobId, cancellationToken);
        logger.LogDebug("Job {JobId} enqueued for execution", jobId);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Jobs worker starting with max {MaxConcurrentJobs} concurrent jobs", 
            _options.MaxConcurrentJobs);

        // Reload running jobs state and populate pending jobs queue on startup
        await ReloadJobsStateAsync(stoppingToken);

        // Keep processing jobs until stopped
        await ProcessPendingJobsAsync(stoppingToken);
    }

    private async Task ReloadJobsStateAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var domainContext = scope.ServiceProvider.GetRequiredService<IDomainContext>();

        // Load pending jobs into the in-memory queue
        var pendingJobIds = await domainContext.Jobs
            .Where(j => j.Status == JobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        foreach (var jobId in pendingJobIds)
        {
            await EnqueueJobAsync(jobId, cancellationToken);
        }
    }
    
    private async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(
            _jobChannel.Reader.ReadAllAsync(stoppingToken),
            new ParallelOptions { MaxDegreeOfParallelism = _options.MaxConcurrentJobs, CancellationToken = stoppingToken },
            async (jobId, ct) =>
            {
                try
                {
                    await StartJobAsync(jobId, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error starting job {JobId}: {Error}", jobId, ex.Message);
                }
            });
    }

    
    private async Task StartJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var domainContext = scope.ServiceProvider.GetRequiredService<IDomainContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var job = await domainContext.Jobs.FindAsync([jobId], cancellationToken);

        if (job == null)
        {
            logger.LogWarning("Job {JobId} not found during execution", jobId);
            return;
        }

        try
        {
            logger.LogInformation("Starting job {JobId} of type {JobType}", job.Id, job.JobType);

            job.Start();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            await ExecuteJobAsync(job, scope.ServiceProvider, cancellationToken);

            job.Complete();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Job {JobId} completed successfully", job.Id);
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message);
            logger.LogError(ex, "Job {JobId} failed with error: {Error}", job.Id, ex.Message);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async ValueTask ExecuteJobAsync(Job jobEntity, IServiceProvider scopedServiceProvider,
        CancellationToken cancellationToken)
    {
        // Get the job type
        var jobType = Type.GetType(jobEntity.JobType);
        if (jobType == null)
        {
            throw new InvalidOperationException($"Job type {jobEntity.JobType} not found");
        }

        // Deserialize the job data
        var jobInstance = jobEntity.JobData.Deserialize(jobType, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (jobInstance is not IJobData job)
        {
            throw new InvalidOperationException($"Job type {jobEntity.JobType} does not implement IJob");
        }
        
        // Create job progress tracker
        var unitOfWork = scopedServiceProvider.GetRequiredService<IUnitOfWork>();
        var executionContext = new JobExecutionContext(jobEntity, unitOfWork, logger);
        
        // Find and execute the handler
        var handlerType = typeof(IJobHandler<>).MakeGenericType(jobType);
        dynamic handler = scopedServiceProvider.GetRequiredService(handlerType);
        await handler.HandleAsync(jobInstance, executionContext, cancellationToken);
    }
}
