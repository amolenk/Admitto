using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobHandler
{
}

public interface IJobHandler<in TJobData> : IJobHandler where TJobData : JobData
{
    ValueTask HandleAsync(TJobData job, IJobExecutionContext executionContext, 
        CancellationToken cancellationToken = default);
}