namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobRunner
{
    ValueTask StartJob(IJob job, CancellationToken cancellationToken = default);
    ValueTask AddOrUpdateScheduledJob(IJob job, string cronExpression, CancellationToken cancellationToken = default);
}