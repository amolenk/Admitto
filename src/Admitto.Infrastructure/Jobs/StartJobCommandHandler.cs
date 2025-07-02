using System.Reflection;
using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class StartJobCommandHandler(
    IJobContext jobContext, 
    IServiceProvider serviceProvider, 
    ILogger<StartJobCommandHandler> logger) : ICommandHandler<StartJobCommand>
{
    public async ValueTask HandleAsync(StartJobCommand command, CancellationToken cancellationToken)
    {
        var job = await jobContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == command.JobId, cancellationToken);

        if (job == null)
        {
            logger.LogWarning("Job {JobId} not found", command.JobId);
            return;
        }

        if (job.Status != JobStatus.Pending)
        {
            logger.LogWarning("Job {JobId} is not in pending status (current: {Status})", 
                command.JobId, job.Status);
            return;
        }

        try
        {
            job.Start();
            await jobContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Starting job {JobId} of type {JobType}", job.Id, job.JobType);

            // Deserialize and execute the job
            await ExecuteJobAsync(job, cancellationToken);

            job.Complete();
            await jobContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Job {JobId} completed successfully", job.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed with error: {Error}", job.Id, ex.Message);

            job.Fail(ex.Message);
            await jobContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async ValueTask ExecuteJobAsync(Job jobEntity, CancellationToken cancellationToken)
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
        var handler = serviceProvider.GetRequiredService(handlerType);

        // Create job progress tracker
        var jobProgress = new JobProgress(jobEntity, jobContext, logger);

        // Get the Handle method and invoke it
        var handleMethod = handlerType.GetMethod("Handle");
        if (handleMethod == null)
        {
            throw new InvalidOperationException($"Handle method not found on {handlerType.Name}");
        }

        var result = handleMethod.Invoke(handler, [job, jobProgress, cancellationToken]);
        
        if (result is ValueTask valueTask)
        {
            await valueTask;
        }
        else if (result is Task task)
        {
            await task;
        }
    }
}