namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IJobHandler
{
    ValueTask RunAsync(CancellationToken cancellationToken);
}
