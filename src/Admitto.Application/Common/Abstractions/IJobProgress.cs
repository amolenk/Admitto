namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobProgress
{
    ValueTask ReportProgressAsync(string message, int? percentComplete = null, CancellationToken cancellationToken = default);
}