// using Amolenk.Admitto.Application.Common.Abstractions;
// using Amolenk.Admitto.Domain.Entities;
// using Microsoft.Extensions.Logging;
//
// namespace Amolenk.Admitto.Infrastructure.Jobs;
//
// public class JobExecutionContext(Job job, IUnitOfWork unitOfWork, ILogger logger) : IJobExecutionContext
// {
//     public async ValueTask ReportProgressAsync(string message, int? percentComplete = null, 
//         CancellationToken cancellationToken = default)
//     {
//         job.UpdateProgress(message, percentComplete);
//         
//         await unitOfWork.SaveChangesAsync(cancellationToken);
//         
//         logger.LogInformation("Job {JobId} progress: {Message} ({Percent}%)", job.Id, message, percentComplete);
//     }
// }