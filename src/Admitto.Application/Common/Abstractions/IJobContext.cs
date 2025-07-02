using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobContext
{
    DbSet<Job> Jobs { get; }
    DbSet<ScheduledJob> ScheduledJobs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}