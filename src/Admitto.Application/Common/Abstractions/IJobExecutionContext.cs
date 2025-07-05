namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobExecutionContext
{
    ValueTask ReportProgressAsync(string message, int? percentComplete = null, CancellationToken cancellationToken = default);
}