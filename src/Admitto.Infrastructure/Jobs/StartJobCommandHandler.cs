using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Jobs;

public class StartJobCommandHandler(
    JobsWorker jobsWorker,
    ILogger<StartJobCommandHandler> logger) : ICommandHandler<StartJobCommand>
{
    public async ValueTask HandleAsync(StartJobCommand command, CancellationToken cancellationToken)
    {
        var executed = await jobsWorker.TryExecuteJob(command.JobId, cancellationToken);
        
        if (executed)
        {
            logger.LogDebug("Job {JobId} handed off to JobsWorker for execution", command.JobId);
        }
        else
        {
            logger.LogDebug("Job {JobId} could not be executed now - will be retried later", command.JobId);
        }
    }
}