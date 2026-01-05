namespace Amolenk.Admitto.Application.Common.Jobs;

public interface IJobHandler
{
    ValueTask RunAsync(CancellationToken cancellationToken);
}
