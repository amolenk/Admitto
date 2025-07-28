namespace Amolenk.Admitto.Application.UseCases.Jobs;

public class StartJobHandler(IJobsWorker jobsWorker) : ICommandHandler<StartJobCommand>
{
    public async ValueTask HandleAsync(StartJobCommand command, CancellationToken cancellationToken)
    {
        await jobsWorker.RunJobAsync(command.JobId, cancellationToken);
    }
}
