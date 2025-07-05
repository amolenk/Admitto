using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobScheduler
{
    ValueTask AddJobAsync<TJobData>(TJobData jobData, CancellationToken cancellationToken = default)
        where TJobData : IJobData;
    
    ValueTask AddOrUpdateRecurringJobAsync(IJobData job, string cronSchedule,
        CancellationToken cancellationToken = default);
}