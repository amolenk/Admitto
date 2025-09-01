// namespace Amolenk.Admitto.Infrastructure.Jobs;
//
// public class JobsOptions
// {
//     public const string SectionName = "Jobs";
//
//     public TimeSpan ScheduledJobsCheckInterval { get; init; } = TimeSpan.FromMinutes(1);
//     public TimeSpan OrphanedJobsCheckInterval { get; init; } = TimeSpan.FromMinutes(5);
//     public TimeSpan OrphanedJobThreshold { get; init; } = TimeSpan.FromMinutes(30);
//     public int MaxConcurrentJobs { get; init; } = Environment.ProcessorCount * 5;
// }