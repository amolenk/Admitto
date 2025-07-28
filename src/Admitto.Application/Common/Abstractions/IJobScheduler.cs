using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobScheduler
{
    ValueTask AddJobAsync<TJobData>(TJobData jobData, CancellationToken cancellationToken = default)
        where TJobData : JobData;
    
    ValueTask AddOrUpdateRecurringJobAsync(JobData job, string cronSchedule,
        CancellationToken cancellationToken = default);
}