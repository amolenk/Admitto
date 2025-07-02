using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class StartJobCommandHandler(
    JobsWorker jobsWorker,
    ILogger<StartJobCommandHandler> logger) : ICommandHandler<StartJobCommand>
{
    public async ValueTask HandleAsync(StartJobCommand command, CancellationToken cancellationToken)
    {
        // Simply enqueue the job for execution - the JobsWorker will handle it
        jobsWorker.EnqueueJob(command.JobId);
        
        logger.LogDebug("Job {JobId} enqueued for execution", command.JobId);
    }
}