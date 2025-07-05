// using Amolenk.Admitto.Application.Common.Abstractions;
// using Cronos;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
//
// namespace Amolenk.Admitto.Infrastructure.Jobs;
//
// public class ScheduledJobsWorker( IServiceProvider serviceProvider, IOptions<JobsOptions> options,
//     ILogger<ScheduledJobsWorker> logger) : BackgroundService
// {
//     private readonly JobsOptions _options = options.Value;
//     
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         while (!stoppingToken.IsCancellationRequested)
//         {
//             try
//             {
//                 await CheckScheduledJobsAsync(stoppingToken);
//             }
//             catch (Exception ex)
//             {
//                 logger.LogError(ex, "Error processing scheduled jobs");
//             }
//
//             await Task.Delay(_options.ScheduledJobsCheckInterval, stoppingToken);
//         }
//     }
//     
//     private async Task CheckScheduledJobsAsync(CancellationToken cancellationToken)
//     {
//         using var scope = serviceProvider.CreateScope();
//         var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
//         var jobRunner = scope.ServiceProvider.GetRequiredService<IJobRunner>();
//         var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();
//
//         var now = DateTimeOffset.UtcNow;
//         var dueJobs = await jobContext.ScheduledJobs
//             .Where(sj => sj.IsEnabled && sj.NextRunTime <= now)
//             .ToListAsync(cancellationToken);
//
//         foreach (var scheduledJob in dueJobs)
//         {
//             try
//             {
//                 logger.LogInformation("Starting scheduled job {JobId} of type {JobType}", 
//                     scheduledJob.Id, scheduledJob.JobType);
//
//                 // Deserialize and start the job
//                 var jobType = Type.GetType(scheduledJob.JobType);
//                 if (jobType == null)
//                 {
//                     logger.LogError("Job type {JobType} not found for scheduled job {JobId}", 
//                         scheduledJob.JobType, scheduledJob.Id);
//                     continue;
//                 }
//
//                 var jobInstance = System.Text.Json.JsonSerializer.Deserialize(
//                     scheduledJob.JobData, jobType, new System.Text.Json.JsonSerializerOptions
//                     {
//                         PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
//                     });
//
//                 if (jobInstance is not IJobData job) continue;
//                 
//                 jobRunner.FireAndForget(job, cancellationToken: cancellationToken);
//
//                 // Update next run time
//                 var cronSchedule = CronExpression.Parse(scheduledJob.CronExpression);
//                 var nextRunTime = cronSchedule.GetNextOccurrence(now.DateTime, TimeZoneInfo.Utc);
//                 if (nextRunTime.HasValue)
//                 {
//                     var nextRunTimeOffset = new DateTimeOffset(nextRunTime.Value, TimeSpan.Zero);
//                     scheduledJob.UpdateNextRunTime(nextRunTimeOffset);
//                 }
//                 else
//                 {
//                     logger.LogWarning("Could not calculate next run time for scheduled job {JobId}", 
//                         scheduledJob.Id);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 logger.LogError(ex, "Error starting scheduled job {JobId}: {Error}", 
//                     scheduledJob.Id, ex.Message);
//             }
//         }
//
//         await unitOfWork.SaveChangesAsync(cancellationToken);
//     }
// }
