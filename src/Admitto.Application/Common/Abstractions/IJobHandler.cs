namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobHandler
{
}

public interface IJobHandler<in TJob> : IJobHandler
    where TJob : IJob
{
    ValueTask Handle(TJob job, IJobProgress jobProgress, CancellationToken cancellationToken = default);
}