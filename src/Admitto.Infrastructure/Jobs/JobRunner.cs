using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;
using Cronos;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class JobRunner(IJobContext jobContext, IMessageOutbox messageOutbox, IUnitOfWork unitOfWork) : IJobRunner
{
    public async ValueTask StartJob(IJob job, CancellationToken cancellationToken = default)
    {
        var jobType = job.GetType().FullName!;
        var jobData = SerializeJobData(job);
        
        var jobEntity = new Job(job.Id, jobType, jobData);
        
        jobContext.Jobs.Add(jobEntity);
        
        // Enqueue job execution message with high priority
        var jobStartCommand = new StartJobCommand(job.Id);
        messageOutbox.Enqueue(jobStartCommand, priority: true);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask AddOrUpdateScheduledJob(IJob job, string cronExpression, CancellationToken cancellationToken = default)
    {
        // Validate cron expression
        var cronSchedule = CronExpression.Parse(cronExpression);
        var nextRunTime = cronSchedule.GetNextOccurrence(DateTimeOffset.UtcNow.DateTime, TimeZoneInfo.Utc);
        if (nextRunTime == null)
        {
            throw new ArgumentException("Invalid cron expression", nameof(cronExpression));
        }
        var nextRunTimeOffset = new DateTimeOffset(nextRunTime.Value, TimeSpan.Zero);

        var jobType = job.GetType().FullName!;
        var jobData = SerializeJobData(job);

        var existingScheduledJob = await jobContext.ScheduledJobs
            .FirstOrDefaultAsync(sj => sj.Id == job.Id, cancellationToken);

        if (existingScheduledJob != null)
        {
            existingScheduledJob.UpdateSchedule(cronExpression, nextRunTimeOffset);
        }
        else
        {
            var scheduledJob = new ScheduledJob(job.Id, jobType, jobData, cronExpression, nextRunTimeOffset);
            jobContext.ScheduledJobs.Add(scheduledJob);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static JsonDocument SerializeJobData(IJob job)
    {
        var options = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };
        return JsonSerializer.SerializeToDocument(job, job.GetType(), options);
    }
}

public record StartJobCommand(Guid JobId) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
}