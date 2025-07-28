namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobsWorker
{
    ValueTask RunJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
