namespace Amolenk.Admitto.Infrastructure.Migrators;

public interface IMigrator
{
    ValueTask RunAsync(CancellationToken cancellationToken);
}