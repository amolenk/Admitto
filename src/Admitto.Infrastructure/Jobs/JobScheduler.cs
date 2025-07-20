using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class JobScheduler(IDomainContext domainContext, JobsWorker jobsWorker, IUnitOfWork unitOfWork) : IJobScheduler
{
    // TODO Could also be part of unit of work... or use this same pattern for the message outbox as well.
    public async ValueTask AddJobAsync<TJobData>(TJobData jobData, CancellationToken cancellationToken = default) 
        where TJobData : IJobData
    {
        var job = Job.Create(jobData);
        
        var existingJob = await domainContext.Jobs.FindAsync([job.Id], cancellationToken);
        if (existingJob is null)
        {
            domainContext.Jobs.Add(job);
        }
        else
        {
            // TODO Log
        }

        // Enqueue the job for processing. To be sure, do this even if the job was already found in the database.
        // The JobsWorker will not restart a completed job.
        unitOfWork.RegisterAfterSaveCallback(() => jobsWorker.EnqueueJobAsync(job.Id, cancellationToken));
    }
    
    public async ValueTask AddOrUpdateRecurringJobAsync(IJobData jobData, string cronExpression, 
        CancellationToken cancellationToken = default)
    {
        // TODO
        throw new NotImplementedException();
        
        // // Validate cron expression
        // var cronSchedule = CronExpression.Parse(cronExpression);
        // var nextRunTime = cronSchedule.GetNextOccurrence(DateTimeOffset.UtcNow.DateTime, TimeZoneInfo.Utc);
        // if (nextRunTime == null)
        // {
        //     throw new ArgumentException("Invalid cron expression", nameof(cronExpression));
        // }
        // var nextRunTimeOffset = new DateTimeOffset(nextRunTime.Value, TimeSpan.Zero);
        //
        // var jobType = job.GetType().FullName!;
        // // var jobData = SerializeJobData(job);
        //
        // var existingScheduledJob = await jobContext.ScheduledJobs.FindAsync([jobData.Id], cancellationToken);
        // if (existingScheduledJob is not null)
        // {
        //     existingScheduledJob.UpdateSchedule(cronExpression, nextRunTimeOffset);
        // }
        // else
        // {
        //     var scheduledJob = new ScheduledJob(job.Id, jobType, jobData, cronExpression, nextRunTimeOffset);
        //     jobContext.ScheduledJobs.Add(scheduledJob);
        // }
    }

    // private static JsonDocument SerializeJobData<TJobData>(TJobData job)
    // {
    //     var options = new JsonSerializerOptions 
    //     { 
    //         PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    //     };
    //     return JsonSerializer.SerializeToDocument(job, job.GetType(), options);
    // }
}
