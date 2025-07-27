using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.UseCases.Jobs;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class JobScheduler(IApplicationContext applicationContext, IMessageOutbox messageOutbox)
    : IJobScheduler
{
    public async ValueTask AddJobAsync<TJobData>(TJobData jobData, CancellationToken cancellationToken = default)
        where TJobData : JobData
    {
        var job = Job.Create(jobData);

        var existingJob = await applicationContext.Jobs.FindAsync([job.Id], cancellationToken);
        if (existingJob is null)
        {
            applicationContext.Jobs.Add(job);
            messageOutbox.Enqueue(new StartJobCommand(job.Id));
        }
        else
        {
            // TODO Log
        }
    }

    public ValueTask AddOrUpdateRecurringJobAsync(
        JobData jobData,
        string cronExpression,
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